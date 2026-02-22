window.getLocation = function (dotNetObjRef) {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function (position) {
            dotNetObjRef.invokeMethodAsync("ReceiveLocation",
                position.coords.latitude,
                position.coords.longitude);
        },
            function (error) {
                console.error("Error getting location:", error);
            });
    } else {
        console.error("Geolocation not supported.");
    }
};
