var zoomLevel = 1;
var isDragging = false;
var startX, startY;
var offsetX = 0, offsetY = 0;

function zoomImage(event) {
    event.preventDefault();
    const lightboxImage = document.getElementById("lightboxImage");

    // Mouse wheel yukarı (zoom in) veya aşağı (zoom out)
    if (event.deltaY < 0) {
        zoomLevel += 0.1;
    } else {
        zoomLevel -= 0.1;
    }

    // Zoom seviyesini sınırlayın (min 1, max 3)
    zoomLevel = Math.max(1, Math.min(zoomLevel, 3));

    // Görüntüyü ölçekleyin
    lightboxImage.style.transform = `scale(${zoomLevel})`;
}
function startDrag(event) {
    event.preventDefault();
    isDragging = true;
    startX = event.clientX - offsetX;
    startY = event.clientY - offsetY;
}

function dragImage(event) {
    if (!isDragging) return;
    event.preventDefault();
    const lightboxImage = document.getElementById("lightboxImage");

    // Yeni konum hesapla
    offsetX = event.clientX - startX;
    offsetY = event.clientY - startY;

    // Görüntüyü hareket ettir
    lightboxImage.style.transform = `scale(${zoomLevel}) translate(${offsetX}px, ${offsetY}px)`;
}

function stopDrag(event) {
    event.preventDefault();
    isDragging = false;
}
document.getElementById("lightboxImage").addEventListener("wheel", zoomImage);

// Mouse drag eventleri
var lightboxImage = document.getElementById("lightboxImage");
lightboxImage.addEventListener("mousedown", startDrag);
lightboxImage.addEventListener("mousemove", dragImage);
lightboxImage.addEventListener("mouseup", stopDrag);
lightboxImage.addEventListener("mouseleave", stopDrag);

document.addEventListener("dragover", (e) => e.preventDefault());
document.addEventListener("drop", (e) => e.preventDefault());

function openLightbox(type, index) {
    currentMediaType = type;
    currentSlideIndex = index;
    document.getElementById("lightbox").style.display = "flex";
    showSlide(currentSlideIndex);
}

function closeLightbox() {
    document.getElementById("lightbox").style.display = "none";
    document.getElementById("lightboxVideo").pause();
    document.getElementById("lightboxImage").src = "";
    document.getElementById("lightboxVideo").src = "";
}
function showSlide(index) {
    var lightboxImage = document.getElementById("lightboxImage");
    var lightboxVideo = document.getElementById("lightboxVideo");

    // Medya türünü ve geçerli listeyi kontrol et
    console.log("currentMediaType:", currentMediaType);

    var currentList = currentMediaType === 'image' ? images : videos;
    console.log("currentList:", currentList);

    // Geçerli indeks kontrolü
    console.log("Initial index:", index);
    if (index >= currentList.length) {
        currentSlideIndex = 0;
    } else if (index < 0) {
        currentSlideIndex = currentList.length - 1;
    } else {
        currentSlideIndex = index;
    }
    console.log("Updated currentSlideIndex:", currentSlideIndex);

    // Geçerli dosyayı al
    var currentFile = currentList[currentSlideIndex];
    console.log("currentFile:", currentFile);

    // Başlık güncellemesi
    document.getElementById("lightboxCaption").innerText = currentFile.caption;
    console.log("Caption:", currentFile.caption);

    // Görsel mi yoksa video mu olduğunu kontrol et ve ilgili elementi göster
    if (currentMediaType === 'image') {
        console.log("Displaying image:", currentFile.src);
        lightboxVideo.style.display = "none";
        lightboxImage.style.display = "block";
        lightboxImage.src = currentFile.src;
    } else {
        console.log("Displaying video:", currentFile.src);
        lightboxImage.style.display = "none";
        lightboxVideo.style.display = "block";
        lightboxVideo.src = currentFile.src;
        lightboxVideo.load();
    }
}

function changeSlide(n) {
    showSlide(currentSlideIndex + n);
}