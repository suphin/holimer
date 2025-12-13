
let _filename;
let _filepath;

let lmap = L.map('lmap',
    {
        fullscreenControl: true,

    }
).setView([38.756, 30.538], 15);

lmap.on('click', function (e) {
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
//getGeoJSONAll('23');

let markersLayer = L.layerGroup().addTo(lmap); // Markerları saklamak için grup
//#region wms
var wms = L.Geoserver.wms("http://192.168.154.127:8080/geoserver/wms", {
    layers: `ne:adalar2`,
    minZoom: 17, // Minimum zoom seviyesi
    maxZoom: 24 // Maksimum zoom seviyesi
});
//wms.addTo(lmap);
var wms2 = L.Geoserver.wms("http://192.168.154.127:8080/geoserver/wms", {
    layers: `ne:parseller2`,
    minZoom: 20, // Minimum zoom seviyesi
    maxZoom: 24 // Maksimum zoom seviyesi
});
//wms2.addTo(lmap); 

var wms4 = L.Geoserver.wms("https://geoserver.afyon.bel.tr/geoserver/wms", {
    layers: `afyon:parselgeo`,
    minZoom: 20, // Minimum zoom seviyesi
    maxZoom: 24 // Maksimum zoom seviyesi
});
wms4.addTo(lmap);

var wms3 = L.Geoserver.wms("https://geoserver.afyon.bel.tr/geoserver/wms", {
    layers: `afyon:bgspyapi`,
    minZoom: 20, // Minimum zoom seviyesi
    maxZoom: 24 // Maksimum zoom seviyesi
});
wms3.addTo(lmap);
var wms5 = L.Geoserver.wms("https://kpsv2.nvi.gov.tr/Services/WMSService/Get?transparent=true", {
    layers: `Yapi`,
    minZoom: 21, // Minimum zoom seviyesi
    maxZoom: 24 // Maksimum zoom seviyesi
});
//wms5.addTo(lmap);
//#endregion


//#region Katman grubu oluşturma






var layers = [
    "Belde", "Belediye", "DevletAlani", "DigerYapi", "DigerYetkiliIdare", "Koy",
    "KoyBaglisi", "Mahalle", "MucavirAlan", "Numarataj", "Yapi", "YetkiAlani",
    "YolOrtaHat", "IdariMerkez", "Il", "IlOzelIdaresi", "Ilce"
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
//L.control.layers(null, layerGroup, { collapsed: false }).addTo(lmap);

// Bir katmanı varsayılan olarak eklemek için (örnek: "Belde")
//layerGroup["Yapi"].addTo(lmap);

//#endregion 

//#region PlugIn -  full screen
lmap.on('fullscreenchange', function () {
    if (lmap.isFullscreen()) {
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
    //  attribution: '© Stadia lmaps, © OpenlmapTiles, © OpenStreetlmap contributors'
});
var darkLayer2 = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
    maxZoom: 20
});


var emptyLayer = L.tileLayer('', {
    attribution: 'Boş Katman'
});
osm.addTo(lmap);

var baseLayers = {
    "Sokak Görünümü": osm,
    "Uydu Görünümü": satelliteLayer,
    "Karanlık Mod": darkLayer,
    "Yükselti ve Sokak": satellite,
    "Karanlık Mod 2": darkLayer2,
    "Yükselti Modu": terrainLayer,
    "Boş Katman": emptyLayer
};

L.control.layers(baseLayers).addTo(lmap);
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
    ]).addTo(lmap);
    lmap.fitBounds(poly.getBounds());
})
    .addTo(lmap);
//#endregion

//#region PlugIn - Çizim Araçları 

// Çizim Katman Grubu
var drawnItems = new L.FeatureGroup();
lmap.addLayer(drawnItems);

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
lmap.addControl(drawControl);

// Çizim Olayları
lmap.on('draw:created', function (event) {
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
lmap.on('draw:edited', function () {
    // alert("Bir şekil düzenlendi!");
});

lmap.on('draw:deleted', function () {
    // alert("Bir şekil silindi!");
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
    downloadAnchor.setAttribute("download", "lmap_data.geojson");
    document.body.appendChild(downloadAnchor); // Gereklidir, yoksa çalışmaz
    downloadAnchor.click();
    downloadAnchor.remove();
}
function saveGeoJSON(id, modulid) {
    if (drawnItems.getLayers().length === 0) {
        alert("Henüz bir çizim yapılmadı!");
        //  return;
    }

    var geojsonData = drawnItems.toGeoJSON();

    fetch('SaveGeoJson?KayitID=' + id + '&ModulID=' + modulid, {
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
function getGeoJSON(id, modulid) {

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
                            document.getElementById("iframeContent").src = '/lmap/VeriGoruntule?KayitID=' + veri.kayitID + '&ModulID=' + veri.modulID;
                        })
                    });
                });
                lmap.fitBounds(geojsonLayer.getBounds());
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

    fetch('map/GetGeoJsonAll?ModulID=' + modulid)
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
                  //  console.log("veri:", veri);
                    // Layer'ı FeatureGroup'a ekle
                    //geojsonLayer.eachLayer(function (layer) {
                    //    drawnItems.addLayer(layer); // drawnItems FeatureGroup'a eklenir
                    //});




                    // GeoJSON Layer'daki her bir layer için işlem yap
                    geojsonLayer.eachLayer(function (layer) {
                        //drawnItems.addLayer(layer); // FeatureGroup içine ekle
                      //  console.log("geojsonLayer:", geojsonLayer);
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
                                    document.getElementById("iframeContent").src = '/lmap/VeriGoruntule?KayitID=' + veri.kayitID + '&ModulID=' + veri.modulID;
                                })
                            });

                            // markerArray.push(marker); // Marker'ı diziye ekle
                        }
                    });

                    overallBounds.extend(geojsonLayer.getBounds());
                });
                lmap.addLayer(markers);



                // Tüm verilerin sınırlarını ayarla
                if (overallBounds.isValid()) {
                    lmap.fitBounds(overallBounds);
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
    fetch('map/GetImageAll?ModulID=' + modulid)
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

                    // marker.addTo(lmap);
                    // Marker'a tıklama olayını ekle
                    marker.on('click', function () {
                        $('#Goruntu360').modal('show');
                        $('#Goruntu360').on('shown.bs.modal', function () {
                            document.getElementById("iframeContent").src = '/DosyaYonetimi/View360?filename=' + layer.dosyaAdi + '&filepath=' + layer.modulDosyaYolu;
                        })
                    });

                    markers.addLayer(marker);
                });
                lmap.addLayer(markers);
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
                    marker.addTo(lmap);
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


function AdaParselAra() {
    var adaparsel = $('#AdaParsel').val();

    $.ajax({
        url: "http://10.0.1.74:88/API/genelAramaYap",
        type: "GET",
        data: {
            text: adaparsel,
            tamEsittir: "false"
        },
        dataType: "json",
        success: function (response) {
            //  console.log(response);
            let dataList = response[0]; // İlk diziyi al
            if (dataList.length > 0) {
                let html = "<h2>Sonuçlar</h2><table class='table table-hover'>";
                dataList.forEach(item => {

                    html += `
                                    <tr>
                                        <td>Mahalle:</td><td>${item.mahalleadi}</td> 
                                        <td>Tapu Alanı:</td> <td>${item.tapu_alan} m²</td>
                                       <td> <button class="btn btn-sm btn-success" onclick="getCoordinates(${item.id})">Haritada Göster</button></td>
                                    </tr>
                                `;
                });

                html += "</table>";
                $("#result").html(html);
                $("#ResultModal").modal('show');

            } else {
                $("#result").html("<p>Sonuç bulunamadı.</p>");
            }
        },
        error: function (xhr, status, error) {
            console.error("Hata:", error);
            $("#result").html("<p>Bir hata oluştu.</p>");
        }
    });
}

function getCoordinates(id) {
    $.ajax({
        url: `http://10.0.1.74:88/API/seciliParselKoordinatGetir?id=${id}`,
        type: "GET",
        dataType: "json",
        success: function (data) {
            console.log(data);

            markersLayer.clearLayers(); // Önceki markerları temizle

            if (data.length > 0) {
                let coords = data.lmap(coord => [coord.y, coord.x]); // Koordinatları diziye çevir

                coords.forEach(coord => {
                    L.marker(coord).addTo(markersLayer)
                        .bindPopup(`<strong>Koordinat:</strong> ${coord[0]}, ${coord[1]}`)
                        .openPopup();
                });

                let bounds = L.latLngBounds(coords);
                lmap.fitBounds(bounds); // Haritayı koordinatlara göre ayarla
            } else {
                alert("Koordinatlar bulunamadı!");
            }
        },
        error: function (xhr, status, error) {
            console.error("Hata:", error);
            alert("Koordinatlar çekilirken hata oluştu.");
        }
    });
}
//#endregion
// Tüm checkbox'ların durumunu kontrol eden değişken
var activeLayers = {};

document.querySelectorAll("input[class='checkbox']").forEach(function (checkbox) {
    checkbox.addEventListener("change", function () {
        var kategoriId = this.getAttribute("data-kategoriId");
        var modulid = this.getAttribute("data-modulId");

        // Eğer daha önce bu kategoriye ait bir katman varsa, onu saklamak için
        if (!activeLayers[kategoriId]) {
            activeLayers[kategoriId] = L.markerClusterGroup();
        }

        var markers = activeLayers[kategoriId];

        if (this.checked) {
            // Checkbox işaretlendiyse GeoJSON verisini çek ve haritaya ekle
            fetch('Map/GetGeoJsonAll?ModulID=' + modulid)
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        var overallBounds = L.latLngBounds();

                        // Gelen data bir liste şeklindeyse, her bir öğeyi döngüyle işleyin
                        data.data.forEach(geoJsonString => {
                            var geojsonLayer = L.geoJSON(JSON.parse(geoJsonString.veri), {
                                onEachFeature: function (feature, layer) {
                                    layer.on('click', function () {
                                        console.log("Feature clicked:", feature);
                                    });
                                }
                            });

                            var veri = geoJsonString;

                            // GeoJSON Layer'daki her bir feature için işlem yap
                            geojsonLayer.eachLayer(function (layer) {
                                if (layer.feature && layer.feature.geometry.type === "Point") {
                                    var coordinates = layer.feature.geometry.coordinates;
                                    var title = layer.feature.properties ? layer.feature.properties.title : "No Title";
                                    var marker = L.marker(new L.LatLng(coordinates[1], coordinates[0]), { title: title });

                                    markers.addLayer(marker);

                                    marker.on('click', function () {
                                        $('#Goruntu360').modal('show');
                                        $('#Goruntu360').on('shown.bs.modal', function () {
                                            document.getElementById("iframeContent").src = '/Map/VeriGoruntule?KayitID=' + veri.kayitID + '&ModulID=' + veri.modulID;
                                        });
                                    });
                                }
                            });

                            overallBounds.extend(geojsonLayer.getBounds());
                        });

                        lmap.addLayer(markers);
                        activeLayers[kategoriId] = markers; // Markerları sakla

                        // Tüm verilerin sınırlarını ayarla
                        if (overallBounds.isValid()) {
                            lmap.fitBounds(overallBounds);
                        }
                    } else {
                        alert('GeoJSON yüklenemedi: ' + data.message);
                    }
                })
                .catch(error => {
                    console.error('Hata:', error);
                });

        } else {
            // Checkbox kaldırıldığında ilgili katmanı haritadan sil
            if (activeLayers[kategoriId]) {
                lmap.removeLayer(activeLayers[kategoriId]);
                delete activeLayers[kategoriId]; // Bellekten de kaldır
            }
        }
    });
});

function toggleAllCheckboxes(selectAllCheckbox) {
    // Tıklanan checkbox'ın data-modulId değerini al
    var modulId = selectAllCheckbox.getAttribute("data-modulId");

    // Aynı modulId'ye sahip tüm checkbox'ları seç
    var allCheckboxes = document.querySelectorAll(`input.checkbox[data-modulId='${modulId}']`);

    // "Hepsini Seç" checkbox'ının durumunu al
    var isChecked = selectAllCheckbox.checked;

    allCheckboxes.forEach(function (checkbox) {
        var kategoriId = checkbox.getAttribute("data-kategoriId"); // Kategori ID'yi al
         
        if (kategoriId && activeLayers[kategoriId]) { 
            lmap.removeLayer(activeLayers[kategoriId]); // Leaflet'ten kaldır
            delete activeLayers[kategoriId]; // Bellekten temizle
        }
        
    });


    allCheckboxes.forEach(function (checkbox) {
        var kategoriId = checkbox.getAttribute("data-kategoriId"); // Kategori ID'yi al

        // Eğer checkbox seçili değilse, ilgili kategoriye ait harita katmanlarını kaldır
        if (!isChecked && kategoriId && activeLayers[kategoriId]) {
            console.log("isChecked:", isChecked);
            console.log("kategoriId:", kategoriId);
            console.log("activeLayers[kategoriId]:", activeLayers[kategoriId]);
            lmap.removeLayer(activeLayers[kategoriId]); // Leaflet'ten kaldır
            delete activeLayers[kategoriId]; // Bellekten temizle
        }

        checkbox.checked = isChecked;
        checkbox.dispatchEvent(new Event("change")); // Change event'ini manuel olarak tetikle
    });
}

