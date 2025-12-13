 
var KTDatatablesExtensionButtons = function () {

	var initTable1 = function () {
		var table = $('#kt_datatable1');
		// begin first table
		table.DataTable({
			responsive: true,
			// Pagination settings
			dom: `<'row'<'col-sm-6 text-left'f><'col-sm-6 text-right'B>>
			<'row'<'col-sm-12'tr>>
			<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7 dataTables_pager'lp>>`,

			buttons: [
				'print',
				'copyHtml5',
				'excelHtml5',
				'csvHtml5',
				'pdfHtml5',
			],
			columnDefs: [
				{
					width: '75px',
					targets: 6,
					render: function (data, type, full, meta) {
						var status = {
							1: { 'title': 'Pending', 'class': 'label-light-primary' },
							2: { 'title': 'Delivered', 'class': ' label-light-danger' },
							3: { 'title': 'Canceled', 'class': ' label-light-primary' },
							4: { 'title': 'Success', 'class': ' label-light-success' },
							5: { 'title': 'Info', 'class': ' label-light-info' },
							6: { 'title': 'Danger', 'class': ' label-light-danger' },
							7: { 'title': 'Warning', 'class': ' label-light-warning' },
						};
						if (typeof status[data] === 'undefined') {
							return data;
						}
						return '<span class="label label-lg font-weight-bold' + status[data].class + ' label-inline">' + status[data].title + '</span>';
					},
				},
				{
					width: '75px',
					targets: 7,
					render: function (data, type, full, meta) {
						var status = {
							1: { 'title': 'Online', 'state': 'danger' },
							2: { 'title': 'Retail', 'state': 'primary' },
							3: { 'title': 'Direct', 'state': 'success' },
						};
						if (typeof status[data] === 'undefined') {
							return data;
						}
						return '<span class="label label-' + status[data].state + ' label-dot mr-2"></span>' +
							'<span class="font-weight-bold text-' + status[data].state + '">' + status[data].title + '</span>';
					},
				},
			],
		});

	};

	 

	return {

		//main function to initiate the module
		init: function () {
			initTable1(); 
		},

	};

}();

jQuery(document).ready(function () {
	KTDatatablesExtensionButtons.init();
});