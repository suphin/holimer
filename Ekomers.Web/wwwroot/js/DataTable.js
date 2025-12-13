var KTDatatablesBasic = function () {

	var initTable1 = function () {
		var table = $('#AnaTablo');

		// begin first table
		table.DataTable({
			responsive: true,
			order:false,
			// DOM Layout settings
			//dom: `<'row'<'col-sm-12'tr>>
			//				<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7 dataTables_pager'lp>>`,

			lengthMenu: [ 25, 50,100,150],

			pageLength: 25,

			language: {
				'lengthMenu': 'Display _MENU_',
			},

		});


	};
	 

	return {

		//main function to initiate the module
		init: function () {
			initTable1(); 

		}
	};
}();

jQuery(document).ready(function () {
	KTDatatablesBasic.init();
});