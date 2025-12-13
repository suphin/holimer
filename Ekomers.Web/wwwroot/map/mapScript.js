
let _filename;
let _filepath;

var map = L.map('map',
    {
        fullscreenControl: true,
       
    }
).setView([41.0402, 29.0237], 15);
 
map.on('click', function (e) {
    /* marker.setLatLng(e.latlng);*/
    $("#Latitude").val(e.latlng.lat);
    $("#Longitude").val(e.latlng.lng);

    const lat = window.parent.document.getElementById("Latitude2");
    
    if (lat) {
        lat.value = e.latlng.lat;
    }
    const lng = window.parent.document.getElementById("Longitude2");
    if (lng) {
        lng.value = e.latlng.lng;
    }
 
});

let markersLayer = L.layerGroup().addTo(map); // Markerları saklamak için grup



//#region Katman grubu oluşturma

 

 


var layers = [
    "Doktor", "Eczane"
];

// 
var layerGroup = {};
layers.forEach(function (layerName) {
    // Her bir katman için rastgele veri oluşturuyoruz (örnek olarak nokta koordinatları ekliyoruz)
    var layer = L.layerGroup([
        L.marker([39.92 + Math.random() * 0.5 - 0.25, 32.85 + Math.random() * 0.5 - 0.25])
            .bindPopup(layerName)
    ]);
    layerGroup[layerName] = layer; // Katmanları grupla
});

// Katman kontrolcüsünü haritaya ekle
//L.control.layers(null, layerGroup, { collapsed: false }).addTo(map);

// Bir katmanı varsayılan olarak eklemek için (örnek: "Belde")
//layerGroup["Yapi"].addTo(map);

//#endregion 

//#region PlugIn -  full screen
map.on('fullscreenchange', function () {
    if (map.isFullscreen()) {
        console.log('entered fullscreen');
    } else {
        console.log('exited fullscreen');
    }
});
//#endregion

//#region Katman kontrolünü haritaya ekleme
var osm = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
});

var satellite = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
});
 

var satelliteLayer = L.tileLayer('https://{s}.google.com/vt/lyrs=s&x={x}&y={y}&z={z}', {
    maxZoom: 20,
    subdomains: ['mt0', 'mt1', 'mt2', 'mt3']
    // attribution: '© Google'
});
var terrainLayer = L.tileLayer('https://{s}.google.com/vt/lyrs=p&x={x}&y={y}&z={z}', {
    maxZoom: 20,
    subdomains: ['mt0', 'mt1', 'mt2', 'mt3']
    // attribution: '© Google'
});
var darkLayer = L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png?api_key=dd6efbf8-de7b-4ff3-9f5b-71b710e60446', {
    maxZoom: 20
    //  attribution: '© Stadia Maps, © OpenMapTiles, © OpenStreetMap contributors'
});
var darkLayer2 = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
    maxZoom: 20
});


var emptyLayer = L.tileLayer('', {
    attribution: 'Boş Katman'
});
osm.addTo(map);

var baseLayers = {
    "Sokak Görünümü": osm,
     "Uydu Görünümü": satelliteLayer,
    "Karanlık Mod": darkLayer,
    "Yükselti ve Sokak": satellite,
    "Karanlık Mod 2": darkLayer2,
    "Yükselti Modu": terrainLayer,
    "Boş Katman": emptyLayer
};

L.control.layers(baseLayers).addTo(map);
//#endregion

//#region PlugIn - Geocoder (Arama) kontrolünü ekleme
var geocoder = L.Control.geocoder({
    defaultMarkGeocode: false // Konuma otomatik işaret koymayı devre dışı bırakır
}).on('markgeocode', function (e) {
        var bbox = e.geocode.bbox;
        var poly = L.polygon([
            [bbox.getSouthWest().lat, bbox.getSouthWest().lng],
            [bbox.getNorthWest().lat, bbox.getNorthWest().lng],
            [bbox.getNorthEast().lat, bbox.getNorthEast().lng],
            [bbox.getSouthEast().lat, bbox.getSouthEast().lng]
        ]).addTo(map);
        map.fitBounds(poly.getBounds());
    })
    .addTo(map);
//#endregion

//#region PlugIn - Çizim Araçları 

// Çizim Katman Grubu
var drawnItems = new L.FeatureGroup();
map.addLayer(drawnItems);

// Çizim Araçları Kontrolü
var drawControl = new L.Control.Draw({
    edit: {
        featureGroup: drawnItems, // Düzenlenebilir özellikler bu grupta olacak
    },
    draw: {
        polyline: true, // Çizgi
        polygon: true, // Alan
        rectangle: true, // Dikdörtgen
        circle: false, // Çember
        marker: true, // Nokta
        circlemarker: false, // circlemarker
    }
});
map.addControl(drawControl);

// Çizim Olayları
map.on('draw:created', function (event) {
    var layer = event.layer;

    // Çizilen şekli katman grubuna ekleyelim
    drawnItems.addLayer(layer);

    // Şekil türüne göre işlem
    if (event.layerType === 'marker') {
      //  alert("Bir nokta çizildi!");
    } else if (event.layerType === 'polyline') {
       // alert("Bir çizgi çizildi!");
    } else if (event.layerType === 'polygon') {
       // alert("Bir alan çizildi!");
    }
});

// Düzenleme veya Silme Olayları
map.on('draw:edited', function () {
   // alert("Bir şekil düzenlendi!");
});

map.on('draw:deleted', function () {
   // alert("Bir şekil silindi!");
});

//#endregion



//#region GeoJSON Yükleme
document.getElementById('fileInput').addEventListener('change', function (event) {
    var file = event.target.files[0]; // Seçilen dosya
    if (file) {
        var reader = new FileReader();
        reader.onload = function (e) {
            var fileContent = e.target.result;
            var fileName = file.name.toLowerCase();

            // Dosya türünü belirle
            if (fileName.endsWith('.kml')) {
                // KML Dosyası Yükle
                try {
                    var kmlLayer = omnivore.kml.parse(fileContent);
                    //kmlLayer.addTo(map);
                    drawnItems.addLayer(kmlLayer);
                    map.fitBounds(kmlLayer.getBounds()); //
                    // Haritayı KML katmanına odakla
                } catch (error) {
                    alert("Hata: KML dosyası işlenemedi.");
                    console.error("KML yükleme hatası:", error);
                }
            } else if (fileName.endsWith('.geojson') || fileName.endsWith('.json')) {
                // GeoJSON Dosyası Yükle
                try {
                    var geojsonData = JSON.parse(fileContent); // GeoJSON'u parse et
                    var geojsonLayer = L.geoJSON(geojsonData, {
                        onEachFeature: function (feature, layer) {
                            // Her bir özellik için tıklama olayı
                            if (feature.properties) {
                                layer.bindPopup(
                                    Object.keys(feature.properties)
                                        .map(key => `<b>${key}:</b> ${feature.properties[key]}`)
                                        .join('<br>')
                                );
                            }
                        },
                        style: function (feature) {
                            // GeoJSON stilleri
                            return {
                                color: 'blue',
                                weight: 2,
                                fillOpacity: 0.5
                            };
                        }
                    }); // GeoJSON katmanı oluştur
                   // geojsonLayer.addTo(map);
                    drawnItems.addLayer(geojsonLayer);
                    map.fitBounds(geojsonLayer.getBounds()); // Haritayı GeoJSON katmanına odakla
                } catch (error) {
                    alert("Hata: GeoJSON dosyası işlenemedi.");
                    console.error("GeoJSON yükleme hatası:", error);
                }
            } else {
                alert("Geçersiz dosya türü. Lütfen KML veya GeoJSON dosyası yükleyin.");
            }
        };
        reader.readAsText(file); // Dosyayı metin olarak oku
    }
});



//#endregion

//#region Fonksiyon Grubu
function downloadGeoJSON() {
    if (drawnItems.getLayers().length === 0) {
        alert("Henüz bir çizim yapılmadı!");
        return;
    }
    var geojsonData = drawnItems.toGeoJSON();
    console.log(geojsonData);
    var dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(geojsonData));
    var downloadAnchor = document.createElement('a');
    downloadAnchor.setAttribute("href", dataStr);
    downloadAnchor.setAttribute("download", "map_data.geojson");
    document.body.appendChild(downloadAnchor); // Gereklidir, yoksa çalışmaz
    downloadAnchor.click();
    downloadAnchor.remove();
}
function saveGeoJSON(id,modulid) {
    if (drawnItems.getLayers().length === 0) {
        alert("Henüz bir çizim yapılmadı!");
      //  return;
    }
    
    var geojsonData = drawnItems.toGeoJSON();

    fetch('SaveGeoJson?KayitID='+id+'&ModulID='+modulid, { 
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(geojsonData)
    })
    .then(response => response.json())
    .then(data => {
        console.log('Başarıyla kaydedildi:', data);
        toastr.success('Kaydetme İşlemi Başarılı', 'Başarılı');
        })
        .catch(error => {
            console.error('Hata:', error);
        });
}
function getGeoJSON(id, modulid)
{
    
    fetch('GetGeoJson?KayitID=' + id + '&ModulID=' + modulid)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                var veri = data.data;
               // console.log("veri:", veri);
                // GeoJSON'u parse ederek FeatureGroup içine ekle
                var geojsonLayer = L.geoJSON(JSON.parse(data.data.veri), {
                    onEachFeature: function (feature, layer) {
                        layer.on('click', function () {
                           // console.log("Feature clicked:", feature);

                        });
                    }
                });
                geojsonLayer.eachLayer(function (layer) {                     
                    drawnItems.addLayer(layer); // FeatureGroup içine ekle
                    layer.on('click', function () {
                        $('#Goruntu360').modal('show');
                        $('#Goruntu360').on('shown.bs.modal', function () {
                            document.getElementById("iframeContent").src = '/Map/VeriGoruntule?KayitID='+veri.kayitID+'&ModulID='+veri.modulID;
                        })
                    });
                });
                map.fitBounds(geojsonLayer.getBounds()); 
            } else {
                //alert('GeoJSON yüklenemedi: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Hata:', error);
           // alert('Bir hata oluştu. Daha sonra tekrar deneyin.');
        });
}
function getGeoJSONAll(modulid) {

    fetch('GetGeoJsonAll?ModulID=' + modulid)
        .then(response => response.json())       
        .then(data => {
            if (data.success) {
                var overallBounds = L.latLngBounds();
                // MarkerClusterGroup oluştur
                var markers = L.markerClusterGroup();
               
                // Gelen data bir liste şeklindeyse, her bir öğeyi döngüyle işleyin
                data.data.forEach(geoJsonString => {
                    // GeoJSON verisini parse edin
                    var geojsonLayer = L.geoJSON(JSON.parse(geoJsonString.veri), {
                        onEachFeature: function (feature, layer) {
                            // Her bir feature için bir olay tanımlayabilirsiniz
                            layer.on('click', function () {
                                console.log("Feature clicked:", feature);
                            });
                        }
                    });
                    var veri = geoJsonString;
                   // console.log("veri:", veri);
                    // Layer'ı FeatureGroup'a ekle
                    //geojsonLayer.eachLayer(function (layer) {
                    //    drawnItems.addLayer(layer); // drawnItems FeatureGroup'a eklenir
                    //});

                   
                   
                  
                    // GeoJSON Layer'daki her bir layer için işlem yap
                    geojsonLayer.eachLayer(function (layer) {
                        //drawnItems.addLayer(layer); // FeatureGroup içine ekle
                        //console.log("geojsonLayer:", geojsonLayer);
                       // console.log("layer:", layer);
                        // Marker oluştur ve cluster'a ekle
                        if (layer.feature && layer.feature.geometry.type === "Point") {
                            var coordinates = layer.feature.geometry.coordinates;
                            var title = layer.feature.properties ? layer.feature.properties.title : "No Title";
                            var marker = L.marker(new L.LatLng(coordinates[1], coordinates[0]), { title: title });
                           // marker.bindPopup(title);
                            markers.addLayer(marker);
                            marker.on('click', function () {
                                $('#Goruntu360').modal('show');
                                $('#Goruntu360').on('shown.bs.modal', function () {
                                    document.getElementById("iframeContent").src = '/Map/VeriGoruntule?KayitID=' + veri.kayitID + '&ModulID=' + veri.modulID;
                                })
                            });

                           // markerArray.push(marker); // Marker'ı diziye ekle
                        }
                    });
                     
                    overallBounds.extend(geojsonLayer.getBounds());
                });
                map.addLayer(markers);



                // Tüm verilerin sınırlarını ayarla
                if (overallBounds.isValid()) {
                    map.fitBounds(overallBounds);
                }
            } else {
                alert('GeoJSON yüklenemedi: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Hata:', error);
           // alert('Bir hata oluştu. Daha sonra tekrar deneyin.');
        });
}
 


 

function getImageAll(modulid) { 
    fetch('GetImageAll?ModulID=' + modulid)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                var markers = L.markerClusterGroup();
                data.data.forEach(layer => {
                      
                    var latlng = [layer.latitude, layer.longitude];
                    var icon = L.divIcon({
                       // className: 'custom-icon',
                        html: '<i class="fas fa-camera icon-2x" style="color:red"></i>',
                        iconSize: [28, 28], // İkon boyutunu ayarlayın
                       // iconAnchor: [15, 15], // İkonun ortasını harita üzerinde işaretleyelim
                        });
                    // Marker'ı haritaya ekle
                    var marker = L.marker(latlng, { icon: icon });

                   // marker.addTo(map);
                    // Marker'a tıklama olayını ekle
                    marker.on('click', function () {
                        $('#Goruntu360').modal('show');
                        $('#Goruntu360').on('shown.bs.modal', function () {
                            document.getElementById("iframeContent").src = '/DosyaYonetimi/View360?filename=' + layer.dosyaAdi + '&filepath=' + layer.modulDosyaYolu;
                        })
                    });

                    markers.addLayer(marker);
                });
                map.addLayer(markers);
            } else {
                alert('Resimler yüklenemedi: ' + data.message);
            }          
        })
        .catch(error => {
            console.error('Hata:', error);
            alert('Bir hata oluştu. Daha sonra tekrar deneyin.');
        });
}
 
function getImage(id, modulid) {
    
    fetch('GetImage?KayitID=' + id + '&ModulID=' + modulid)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
 
                data.data.forEach(layer => {
                      
                    var latlng = [layer.latitude, layer.longitude];
                    var icon = L.divIcon({
                        // className: 'custom-icon',
                        html: '<i class="fas fa-camera icon-2x" style="color:red"></i>',
                        iconSize: [28, 28], // İkon boyutunu ayarlayın
                        // iconAnchor: [15, 15], // İkonun ortasını harita üzerinde işaretleyelim
                    });
                    // Marker'ı haritaya ekle
                    var marker = L.marker(latlng, { icon: icon });
                    marker.addTo(map);
                    // Marker'a tıklama olayını ekle
                    marker.on('click', function () {
                        $('#Goruntu360').modal('show');
                        $('#Goruntu360').on('shown.bs.modal', function () {
                            document.getElementById("iframeContent").src = '/DosyaYonetimi/View360?filename=' + layer.dosyaAdi + '&filepath=' + layer.modulDosyaYolu;
                        })
                    });
                });


            } else {
                alert('Resimler yüklenemedi: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Hata:', error);
            alert('Bir hata oluştu. Daha sonra tekrar deneyin.');
        });
}

//#endregion