// Clicking a row in the Tugas / Riwayat tables navigates to its data-url,
// so a whole row acts as a link instead of just the text inside it.
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".task-row").forEach(function (row) {
        row.addEventListener("click", function () {
            const url = this.dataset.url;

            if (url) {
                window.location.href = url;
            }
        });
    });
});