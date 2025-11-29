// Make table rows clickable
document.querySelectorAll('.clickable-row').forEach(row => {
    row.addEventListener('click', function() {
        window.location.href = this.dataset.href;
    });
});

function printAllBarcodes() {
    // Clone the print area
    var printContent = document.getElementById('printArea').innerHTML;
    
    // Create a new window for printing
    var printWindow = window.open('', '_blank');
    printWindow.document.write('<html><head><title>Print Barcodes</title>');
    printWindow.document.write('<style>');
    printWindow.document.write('@@page { size: auto; margin: 10mm; }');
    printWindow.document.write('body { margin: 0; font-family: Arial, sans-serif; }');
    printWindow.document.write('.print-container { display: flex; flex-wrap: wrap; gap: 15px; }');
    printWindow.document.write('.print-barcode-card { page-break-inside: avoid; width: calc(50% - 10px); border: 1px solid #ddd; padding: 15px; text-align: center; box-sizing: border-box; }');
    printWindow.document.write('.print-barcode-card h4 { margin: 10px 0 5px 0; font-size: 16px; }');
    printWindow.document.write('.print-barcode-card p { margin: 5px 0; font-size: 12px; }');
    printWindow.document.write('.print-barcode-card img { max-width: 100%; height: auto; }');
    printWindow.document.write('</style>');
    printWindow.document.write('</head><body>');
    printWindow.document.write(printContent);
    printWindow.document.write('</body></html>');
    printWindow.document.close();
    
    // Wait for images to load before printing
    printWindow.onload = function() {
        printWindow.focus();
        printWindow.print();
        printWindow.close();
    };
}
