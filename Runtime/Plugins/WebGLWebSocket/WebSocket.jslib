var LibraryWebSocket = {
	$webSocketState: {
		/*
		 * Map of instances
		 *
		 * Instance structure:
		 * {
		 * 	url: string,
		 * 	ws: WebSocket
		 * }
		 */
		instances: [],

		/* Last instance ID */
		lastId: 0,

		/* Event listeners */
		onOpen: null,
		onMesssage: null,
		onError: null,
		onClose: null,

		/* Debug mode */
		debug: false
	},

	/**
	 * Set onOpen callback
	 *
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnOpen: function(callback) {
		webSocketState.onOpen = callback;
	},

	/**
	 * Set onMessage callback
	 *
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnMessage: function(callback) {
		webSocketState.onMessage = callback;
	},

	/**
	 * Set onError callback
	 *
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnError: function(callback) {
		webSocketState.onError = callback;
	},

	/**
	 * Set onClose callback
	 *
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnClose: function(callback) {
		webSocketState.onClose = callback;
	},

	/**
	 * Allocate new WebSocket instance struct
	 *
	 * @param url Server URL
	 */
	WebSocketAllocate: function() {
		var id = webSocketState.instances.push({
			subprotocols: [],
			url: null,
			ws: null
		}) - 1;
		return id;
	},

	/**
	 * Add subprotocol to instance
	 *
	 * @param instanceId Instance ID
	 * @param subprotocol Subprotocol name to add to instance
	 */
	WebSocketAddSubProtocol: function(instanceId, subprotocol) {
		var subprotocolStr = UTF8ToString(subprotocol);
		webSocketState.instances[instanceId].subprotocols.push(subprotocolStr);
	},

	/**
	 * Remove reference to WebSocket instance
	 *
	 * If socket is not closed function will close it but onClose event will not be emitted because
	 * this function should be invoked by C# WebSocket destructor.
	 *
	 * @param instanceId Instance ID
	 */
	WebSocketFree: function(instanceId) {
		var instance = webSocketState.instances[instanceId];

		if (!instance) return 0;

		// Close if not closed
		if (instance.ws && instance.ws.readyState < 2)
			instance.ws.close();

		// Remove reference
		delete webSocketState.instances[instanceId];

		return 0;
	},

	/**
	 * Connect WebSocket to the server
	 *
	 * @param instanceId Instance ID
	 */
	WebSocketConnect: function(url, instanceId, onOpen, onMessage, onError, onClose) {
		var instance = webSocketState.instances[instanceId];
		instance.url = UTF8ToString(url);
		if (!instance) return -1;

		if (instance.ws !== null)
			return -2;

		instance.ws = new WebSocket(instance.url, instance.subprotocols);

		instance.ws.binaryType = 'arraybuffer';

		instance.ws.onopen = function() {
			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Connected.");

			dynCall_vi(onOpen, instanceId);
		};

		instance.ws.onmessage = function(ev) {
			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Received message:", ev.data);

			if (ev.data instanceof ArrayBuffer) {
				var dataBuffer = new Uint8Array(ev.data);

				var buffer = _malloc(dataBuffer.length);
				HEAPU8.set(dataBuffer, buffer);

				try {
					dynCall_viiii(onMessage, instanceId, buffer, dataBuffer.length, false);
				} finally {
					_free(buffer);
				}
			} else {
				var dataBuffer = (new TextEncoder()).encode(ev.data);

				var buffer = _malloc(dataBuffer.length);
				HEAPU8.set(dataBuffer, buffer);

				try {
					dynCall_viiii(onMessage, instanceId, buffer, dataBuffer.length, true);
				} finally {
					_free(buffer);
				}
			}
		};

		instance.ws.onerror = function(ev) {
			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Error occured.");

			var msg = "WebSocket error.";
			var msgBytes = lengthBytesUTF8(msg);
			var msgBuffer = _malloc(msgBytes + 1);
			stringToUTF8(msg, msgBuffer, msgBytes);

			try {
				dynCall_vii(onError, instanceId, msgBuffer);
			} finally {
				_free(msgBuffer);
			}
		};

		instance.ws.onclose = function(ev) {
			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Closed.");

			dynCall_vii(onClose, instanceId, ev.code);

			delete instance.ws;
		};
		return 0;
	},

	/**
	 * Close WebSocket connection
	 *
	 * @param instanceId Instance ID
	 * @param code Close status code
	 * @param reasonPtr Pointer to reason string
	 */
	WebSocketClose: function(instanceId, code, reasonPtr) {
		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState === 2)
			return -4;

		if (instance.ws.readyState === 3)
			return -5;

		var reason = ( reasonPtr ? UTF8ToString(reasonPtr) : undefined );

		try {
			instance.ws.close(code, reason);
		} catch(err) {
			return -7;
		}

		return 0;
	},

	/**
	 * Send message over WebSocket
	 *
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 */
	WebSocketSend: function(instanceId, bufferPtr, length) {
		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		instance.ws.send(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));

		return 0;
	},

	/**
	 * Send message over WebSocket
	 *
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 * @param endOfMessage is this the end of the message
	 */
	WebSocketSendFragment: function(instanceId, bufferPtr, length, endOfMessage) {
		var options = new Object();
		options.fin = endOfMessage;
		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		instance.ws.send(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length), options);

		return 0;
	},

	/**
	 * Send text message over WebSocket
	 *
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 */
	WebSocketSendText: function(instanceId, message) {
		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		instance.ws.send(UTF8ToString(message));

		return 0;
	},

	/**
	 * Return WebSocket readyState
	 *
	 * @param instanceId Instance ID
	 */
	WebSocketGetState: function(instanceId) {
		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws)
			return instance.ws.readyState;
		else
			return 3;
	}
};

autoAddDeps(LibraryWebSocket, '$webSocketState');
mergeInto(LibraryManager.library, LibraryWebSocket);
