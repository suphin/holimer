map.addControl(new ZoomControl(), 'bottom-right');

map.addControl(new CompassControl({ instant: true }), 'bottom-right');

map.addControl(new InspectControl({ console: true }), 'bottom-right');

map.addControl(new RulerControl(), 'bottom-right');
map.on('ruler.on', () => console.log('Ruler activated'));
map.on('ruler.off', () => console.log('Ruler deactivated'));

//const image = new ImageControl({ removeButton: true });
//map.addControl(image, 'bottom-right');
//map.on('image.add', ({ id }) => console.log(`Added image ${id}`));
//map.on('image.remove', ({ id }) => console.log(`Removed image ${id}`));
//map.on('image.select', ({ id }) => console.log(`Selected image ${id}`));
//map.on('image.deselect', ({ id }) => console.log(`Deselected image ${id}`));
//map.on('image.update', ({ coordinates }) => console.log('Updated position:', coordinates));
//map.on('image.mode', ({ mode }) => console.log(`Changed mode: ${mode}`));
 

map.addControl(new TooltipControl({
	layer: 'polygon-fill',
	getContent: (event) => {
		console.log('Tooltip for feature id:', event.features?.at(0).id);
		return `TooltipControl example ${event.lngLat.lng.toFixed(6)}, ${event.lngLat.lat.toFixed(6)}`;
	},
}));

const languageControl = new LanguageControl();
map.addControl(languageControl);
document.getElementById('languages').addEventListener('change', (event) => {
	languageControl.setLanguage(event.target.value);
});

map.addControl(new StylesControl(), 'top-left');
map.addControl(new StylesControl({ compact: true }), 'top-left');