function Yazdir(tableid) {
	
	//$.print("#printable"); //Yazdırma işlemini burada tetikliyoruz.
	
		$(tableid).print({
			//Use Global styles
			globalStyles: true,
			//Add link with attrbute media=print
			mediaPrint: false,
			//Custom stylesheet
			stylesheet: "http://fonts.googleapis.com/css?family=Inconsolata",
			//Print in a hidden iframe
			iframe: false,
			//Don't print this
			noPrintSelector: ".no-print",
			//Add this at top
			//prepend: "<br /><p>YAZDIRAN: "+ User +" </p>",
			//Add this on bottom
			append: null,
			//Log to console when printing is done via a deffered callback
			deferred: $.Deferred().done(function () { console.log('Printing done', arguments); })
		});
	
}


function TableToExcel(tableid) {
	//$.print("#printable"); //Yazdırma işlemini burada tetikliyoruz.
	$(document).ready(function () {

		$(tableid).table2excel({
			exclude: ".noExl",
			name: "Worksheet Name",
			filename: "SomeFile" //do not include extension
		});
	});

}

function removeZeroValueOptions(selectElementId) {
	var selectElement = document.getElementById(selectElementId);
	var options = selectElement.options;
	 
	for (var i = options.length - 1; i >= 0; i--) {
		if (options[i].value == "0") {
			selectElement.remove(i);
		}
	}
}

function IlkHarfBuyukYap(TextElement) {
	var Input = document.getElementById(TextElement);

	Input.addEventListener("blur", function () {
		var words = Input.value.toLowerCase().split(' ');
		for (var i = 0; i < words.length; i++) {
			if (words[i].length > 0) {
				words[i] = words[i][0].toUpperCase() + words[i].substring(1);
			}
		}
		Input.value = words.join(' ');
	});
}

//const fullscreenButton = document.getElementById('fullscreenButton');
//const myCard = document.getElementById('kt_page_sticky_card');

//fullscreenButton.addEventListener('click', () => {
//	if (!document.fullscreenElement) {
//		myCard.requestFullscreen().catch(err => {
//			alert(`Error attempting to enable full-screen mode: ${err.message}`);
//		});
//	} else {
//		document.exitFullscreen();
//	}
//});

//// Optional: Update button text/icon based on fullscreen state
//document.addEventListener('fullscreenchange', () => {
//	if (document.fullscreenElement) {
//		fullscreenButton.innerHTML = '<i class="bi bi-arrows-collapse"></i> Çıkış';
//		myCard.classList.add('fullscreen-enabled');
//	} else {
//		fullscreenButton.innerHTML = '<i class="bi bi-arrows-fullscreen"></i> Tam Ekran';
//		myCard.classList.remove('fullscreen-enabled');
//	}
//});

function fullscreen(id) {
	const myCard = document.getElementById(id);

	 
		if (!document.fullscreenElement) {
			myCard.requestFullscreen().catch(err => {
				alert(`Error attempting to enable full-screen mode: ${err.message}`);
			});
		} else {
			document.exitFullscreen();
		}
 

	// Optional: Update button text/icon based on fullscreen state
	document.addEventListener('fullscreenchange', () => {
		if (document.fullscreenElement) {
			fullscreenButton.innerHTML = '<i class="bi bi-arrows-collapse"></i> Çıkış';
			myCard.classList.add('fullscreen-enabled');
		} else {
			fullscreenButton.innerHTML = '<i class="bi bi-arrows-fullscreen"></i> Tam Ekran';
			myCard.classList.remove('fullscreen-enabled');
		}
	});
}
 