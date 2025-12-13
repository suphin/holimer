import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
/** CompassControl */
import CompassControl from '@mapbox-controls/compass';
import '@mapbox-controls/compass/src/index.css';
/** ImageControl */
import ImageControl from '@mapbox-controls/image';
import '@mapbox-controls/image/src/index.css';
/** InspectControl */
import InspectControl from '@mapbox-controls/inspect';
import '@mapbox-controls/inspect/src/index.css';
/** LanguageControl */
import LanguageControl from '@mapbox-controls/language';
/** RulerControl */
import RulerControl from '@mapbox-controls/ruler';
import '@mapbox-controls/ruler/src/index.css';
/** StylesControl */
import StylesControl from '@mapbox-controls/styles';
import '@mapbox-controls/styles/src/index.css';
/** TooltipControl */
import TooltipControl from '@mapbox-controls/tooltip';
import '@mapbox-controls/tooltip/src/index.css';
/** ZoomControl */
import ZoomControl from '@mapbox-controls/zoom';
import '@mapbox-controls/zoom/src/index.css';

const polygon = {
	id: 1234567890,
	type: 'Feature',
	properties: {},
	geometry: {
		type: 'Polygon',
		coordinates: [
			[
				[30.51611423492432, 50.452667766971196],
				[30.514655113220215, 50.449006093706274],
				[30.516843795776367, 50.44862351447756],
				[30.518345832824707, 50.45217591688964],
				[30.51611423492432, 50.452667766971196],
			],
		],
	},
};

const map = new mapboxgl.Map({
	accessToken: 'pk.eyJ1Ijoia29yeXdrYSIsImEiOiJja2p1ajdlOWozMnF2MzBtajRvOTVzZDRpIn0.nnlX7TDuZ3zuGkZGr_oA3A',
	container: 'map',
	style: 'mapbox://styles/mapbox/standard',
	zoom: 14,
	center: [30.5234, 50.4501],
});

map.on('style.load', () => {
	map.addLayer({
		id: 'polygon-fill',
		type: 'fill',
		source: { type: 'geojson', data: polygon },
		paint: { 'fill-opacity': 0.2, 'fill-color': '#4264fb' },
	});
	map.addLayer({
		id: 'polygon-line',
		type: 'line',
		source: { type: 'geojson', data: polygon },
		paint: { 'line-width': 2, 'line-color': '#4264fb' },
	});
});

map.addControl(new ZoomControl(), 'bottom-right');

map.addControl(new CompassControl({ instant: true }), 'bottom-right');

map.addControl(new InspectControl({ console: true }), 'bottom-right');

map.addControl(new RulerControl(), 'bottom-right');
map.on('ruler.on', () => console.log('Ruler activated'));
map.on('ruler.off', () => console.log('Ruler deactivated'));

const image = new ImageControl({ removeButton: true });
map.addControl(image, 'bottom-right');
map.on('image.add', ({ id }) => console.log(`Added image ${id}`));
map.on('image.remove', ({ id }) => console.log(`Removed image ${id}`));
map.on('image.select', ({ id }) => console.log(`Selected image ${id}`));
map.on('image.deselect', ({ id }) => console.log(`Deselected image ${id}`));
map.on('image.update', ({ coordinates }) => console.log('Updated position:', coordinates));
map.on('image.mode', ({ mode }) => console.log(`Changed mode: ${mode}`));

(async function () {
	await map.once('style.load');
	await image.addUrl('/maps/map1-11.png', [
		//x y
		[30.51943319201746, 38.7734560],//1 460

		[30.51943319201746, 38.7684609],//2 509

		[30.510328159207575, 38.7684609],//3 682

		[30.510328159207575, 38.7734560],//4 556


	]);

	map.on('image.select', ({ id }) => {
		const rasterLayerId = image.rasters[id].rasterLayer.id;
		const range = document.createElement('input');
		range.style.position = 'absolute';
		range.style.left = '50%';
		range.style.transform = 'translateX(-50%)';
		range.style.bottom = '16px';
		range.type = 'range';
		range.min = 0;
		range.step = 0.05;
		range.max = 1;
		range.value = map.getPaintProperty(rasterLayerId, 'raster-opacity');
		range.addEventListener('input', () => {
			map.setPaintProperty(rasterLayerId, 'raster-opacity', Number(range.value));
		});
		document.body.appendChild(range);
		map.once('image.deselect', () => {
			document.body.removeChild(range);
		});

		// Modal'ý göster
		const modal = document.getElementById('ModalMap');
		const modalBody = document.getElementById('modal-body');


		const info = document.getElementById('info');
		//const ada = document.getElementById('Ada');
		//ada.value = id;


		//alert(id);
		modal.style.display = 'block';

	});
})();


(async function () {
	await map.once('style.load');
	await image.addUrl('/maps/map1-2.png', [
		//x y
		[30.5243272, 38.7735320],//1 460		X2,Y2

		[30.5243272, 38.7683307],//2 509		X2,Y1

		[30.5163282, 38.7683407],//3 682	X1,Y1

		[30.5163282, 38.7735420],//4 556	X1,Y2


	]);

	map.on('image.select', ({ id }) => {
		const rasterLayerId = image.rasters[id].rasterLayer.id;
		const range = document.createElement('input');
		range.style.position = 'absolute';
		range.style.left = '50%';
		range.style.transform = 'translateX(-50%)';
		range.style.bottom = '16px';
		range.type = 'range';
		range.min = 0;
		range.step = 0.05;
		range.max = 1;
		range.value = map.getPaintProperty(rasterLayerId, 'raster-opacity');
		range.addEventListener('input', () => {
			map.setPaintProperty(rasterLayerId, 'raster-opacity', Number(range.value));
		});
		document.body.appendChild(range);
		map.once('image.deselect', () => {
			document.body.removeChild(range);
		});

		// Modal'ý göster
		const modal = document.getElementById('ModalMap');
		const modalBody = document.getElementById('modal-body');


		const info = document.getElementById('info');
		//const ada = document.getElementById('Ada');
		//ada.value = id;


		//alert(id);
		modal.style.display = 'block';

	});
})();

(async function () {
	await map.once('style.load');
	await image.addUrl('/maps/zemin.jpg', [
		//x y

		[30.5268716638366, 38.77529557378065],//1 

		[30.527125896816955, 38.7669401043116],//2  

		[30.50953493014464, 38.76691723215819],//3 

		[30.509339366312247, 38.77604263925966],//4  

	]);

	map.on('image.select', ({ id }) => {
		const rasterLayerId = image.rasters[id].rasterLayer.id;
		const range = document.createElement('input');
		range.style.position = 'absolute';
		range.style.left = '50%';
		range.style.transform = 'translateX(-50%)';
		range.style.bottom = '16px';
		range.type = 'range';
		range.min = 0;
		range.step = 0.05;
		range.max = 1;
		range.value = map.getPaintProperty(rasterLayerId, 'raster-opacity');
		range.addEventListener('input', () => {
			map.setPaintProperty(rasterLayerId, 'raster-opacity', Number(range.value));
		});
		document.body.appendChild(range);
		map.once('image.deselect', () => {
			document.body.removeChild(range);
		});

		// Modal'ý göster
		const modal = document.getElementById('ModalMap');
		const modalBody = document.getElementById('modal-body');


		const info = document.getElementById('info');
		//const ada = document.getElementById('Ada');
		//ada.value = id;


		//alert(id);
		modal.style.display = 'block';

	});
})();

map.addControl(new TooltipControl({
	layer: 'polygon-fill',
	getContent: (event) => {
		console.log('Tooltip for feature id:', event.features?.at(0).id);
		return `TooltipControl example ${event.lngLat.lng.toFixed(6)}, ${event.lngLat.lat.toFixed(6)}`;
	},
}));

//const languageControl = new LanguageControl();
//map.addControl(languageControl);
//document.getElementById('languages').addEventListener('change', (event) => {
//	languageControl.setLanguage(event.target.value);
//});

map.addControl(new StylesControl(), 'top-left');
map.addControl(new StylesControl({ compact: true }), 'top-left');