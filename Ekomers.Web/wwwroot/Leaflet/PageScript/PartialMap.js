const toggleViewButton = document.getElementById('toggle-view');
const nextViewButton = document.getElementById('next-view');
const streetView = document.getElementById('street-view');
const mapFrame = document.getElementById('mapFrame');



nextViewButton.addEventListener('click', () => {
	 

		const lat = window.parent.document.getElementById("Latitude2").value;// 38.756;
		const lng = window.parent.document.getElementById("Longitude2").value;// 30.538;

		console.log(lat);
		console.log(lng);

		streetView.src = `https://www.google.com/maps/embed?pb=!1m0!4v1623797267247!6m8!1m7!1sCAoSLEFGMVFpcE0zWF9tbjNqVUVhR1IxNEw3RzQ2OXNaMEd2Y2ZBb2xPOVBEU0Qw!2m2!1d${lat}!2d${lng}!3f0!4f0!5f0.7820865974627469`;
 
});







toggleViewButton.addEventListener('click', () => {
	if (streetView.style.display == 'none') {
		// Street View'u aç
		streetView.style.display = 'block';
		nextViewButton.style.display = 'block';
		mapFrame.style.flex = '1';  

		const lat = window.parent.document.getElementById("Latitude2").value;// 38.756;
		const lng = window.parent.document.getElementById("Longitude2").value;// 30.538;
		 

		streetView.src = `https://www.google.com/maps/embed?pb=!1m0!4v1623797267247!6m8!1m7!1sCAoSLEFGMVFpcE0zWF9tbjNqVUVhR1IxNEw3RzQ2OXNaMEd2Y2ZBb2xPOVBEU0Qw!2m2!1d${lat}!2d${lng}!3f0!4f0!5f0.7820865974627469`;

	 toggleViewButton.innerHTML = 'Street View Gizle <i class="icon-x fas fa-street-view"></i>';
	} else {
		// Street View'u kapat38.756, 30.538
		streetView.style.display = 'none';
		nextViewButton.style.display = 'none';
		mapFrame.style.flex = '1';  
		toggleViewButton.innerHTML = 'Street View Göster <i class="icon-x fas fa-street-view"></i>';
	}
});