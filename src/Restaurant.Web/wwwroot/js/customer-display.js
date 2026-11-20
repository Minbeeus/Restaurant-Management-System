window.CustomerDisplay = {
    _dotNetHelper: null,
    _channel: null,

    init: function (dotNetHelper) {
        window.CustomerDisplay._dotNetHelper = dotNetHelper;

        // Check if explicitly launched with ?showOrder=... from POS
        try {
            const params = new URLSearchParams(window.location.search);
            const showOrder = params.get('showOrder');
            if (showOrder) {
                const orderId = parseInt(showOrder, 10);
                if (orderId > 0) {
                    dotNetHelper.invokeMethodAsync('OnShowOrderFromJs', orderId);
                }
                // Immediately clean URL so refresh will always be standby welcome
                window.history.replaceState({}, document.title, window.location.pathname);
            }
        } catch (e) {
            console.warn('URL param parse error:', e);
        }

        if (window.BroadcastChannel) {
            try {
                if (window.CustomerDisplay._channel) {
                    window.CustomerDisplay._channel.close();
                }
                const bc = new BroadcastChannel('pizza_corner_customer_display');
                bc.onmessage = function (ev) {
                    if (ev.data) {
                        if (ev.data.action === 'SHOW_ORDER' && ev.data.orderId) {
                            dotNetHelper.invokeMethodAsync('OnShowOrderFromJs', ev.data.orderId);
                        } else if (ev.data.action === 'SHOW_WELCOME') {
                            dotNetHelper.invokeMethodAsync('OnResetDisplayFromJs');
                        }
                    }
                };
                window.CustomerDisplay._channel = bc;
            } catch (e) {
                console.warn('BroadcastChannel error:', e);
            }
        }

        window.addEventListener('storage', function (ev) {
            if (ev.key === 'pizza_corner_customer_display_event' && ev.newValue) {
                try {
                    const data = JSON.parse(ev.newValue);
                    if (data.action === 'SHOW_ORDER' && data.orderId) {
                        dotNetHelper.invokeMethodAsync('OnShowOrderFromJs', data.orderId);
                    } else if (data.action === 'SHOW_WELCOME') {
                        dotNetHelper.invokeMethodAsync('OnResetDisplayFromJs');
                    }
                } catch (e) { }
            }
        });
    },

    broadcast: function (action, orderId) {
        try {
            if (window.BroadcastChannel) {
                const bc = new BroadcastChannel('pizza_corner_customer_display');
                bc.postMessage({ action: action, orderId: orderId, timestamp: Date.now() });
                bc.close();
            }
            localStorage.setItem('pizza_corner_customer_display_event', JSON.stringify({
                action: action,
                orderId: orderId,
                timestamp: Date.now()
            }));
        } catch (e) {
            console.warn('CustomerDisplay.broadcast error:', e);
        }
    },

    openWindow: function (url) {
        try {
            const win = window.open(url, 'PizzaCorner_CustomerDisplay', 'width=1024,height=768,menubar=no,toolbar=no,location=no,status=no');
            if (win && win.focus) {
                win.focus();
            }
        } catch (e) {
            console.warn('CustomerDisplay.openWindow error:', e);
        }
    }
};
