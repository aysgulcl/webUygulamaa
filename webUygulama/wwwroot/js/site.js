// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Sepet işlemleri için fonksiyonlar
function updateCartCount() {
    fetch('/Cart/GetCartCount')
        .then(response => response.json())
        .then(data => {
            const cartCountElement = document.getElementById('cartCount');
            if (cartCountElement) {
                cartCountElement.textContent = data > 0 ? data : '';
            }
        });
}

function addToCart(eventId, ticketType, quantity) {
    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            eventId: eventId,
            ticketType: ticketType,
            quantity: quantity
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            Swal.fire({
                title: 'Başarılı!',
                text: 'Bilet sepete eklendi',
                icon: 'success',
                confirmButtonText: 'Tamam'
            });
            updateCartCount();
        } else {
            Swal.fire({
                title: 'Hata!',
                text: data.message || 'Bir hata oluştu',
                icon: 'error',
                confirmButtonText: 'Tamam'
            });
        }
    });
}

// Sayfa yüklendiğinde sepet sayısını güncelle
document.addEventListener('DOMContentLoaded', function() {
    updateCartCount();
});
