$(document).ready(function () {
	// DataTable'ý baþlat
	var table = $('#kt_datatable1').DataTable({
		responsive: true,
		processing: true,
		lengthMenu: [10,50,100,1000],
		// Pagination settings
		dom: `<'row'<'col-sm-6 text-left hide'f><'col-sm-6 text-right hide'B>>
					<'row'<'col-sm-12'tr>>
					<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7 dataTables_pager'lp>>`,
		buttons: [
			{
				extend: 'print',
				text: 'Yazdýr',
				exportOptions: {
					columns: ':not(.no-print)' // 
				}
			},
			{
				extend: 'copyHtml5',
				text: 'Kopyala',
				exportOptions: {
					columns: ':not(.no-print)' //
				}
			},
			{
				extend: 'excelHtml5',
				text: 'Excel',
				exportOptions: {
					columns: ':not(.no-print)' // 
				}
			},
			{
				extend: 'csvHtml5',
				text: 'CSV',
				exportOptions: {
					columns: ':not(.no-print)' //
				}
			},
			{
				extend: 'pdfHtml5',
				text: 'PDF',
				exportOptions: {
					columns: ':not(.no-print)' // 
				}
			},
		]
	});


	// $('#export_copy').on('click', function (e) {
	// 	e.preventDefault();
	// 	table.button(1).trigger();
	// });
	// Manuel olarak buton olaylarýný baðla
	$('#printButton').on('click', function () {
		table.button('.buttons-print').trigger();
	});
	$('#copyButton').on('click', function () {
		table.button('.buttons-copy').trigger();
	});
	$('#excelButton').on('click', function () {
		table.button('.buttons-excel').trigger();
	});

	$('#csvButton').on('click', function () {
		table.button('.buttons-csv').trigger();
	});

	$('#pdfButton').on('click', function () {
		table.button('.buttons-pdf').trigger();
	});
});
