 
    function initMap () {
        // define leaflet
        var leaflet = L.map('kt_leaflet_1', {
            center: [38.75570783985728, 30.53581173821712],
            zoom: 14
        });

        // set leaflet tile layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
           // attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(leaflet);
         
       

        
    }
 

document.addEventListener("DOMContentLoaded", function () {
    initMap();
    document.getElementsByClassName("leaflet-bottom leaflet-right")[0].style.display = "none";
});
 
 
 
 
 
 
 
 