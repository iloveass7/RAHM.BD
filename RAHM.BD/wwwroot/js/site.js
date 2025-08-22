(function () {
    const slider = document.getElementById('heroSlider');
    if (!slider) return;

    const track = slider.querySelector('.slides');
    const slides = Array.from(slider.querySelectorAll('.slide'));
    const dots = slider.querySelector('#heroDots');

    let index = 0;
    const count = slides.length;
    const intervalMs = 5000;

    slides.forEach((_, i) => {
        const b = document.createElement('button');
        b.addEventListener('click', () => goTo(i));
        dots.appendChild(b);
    });

    const dotEls = Array.from(dots.children);
    function update() {
        track.style.transform = `translateX(-${index * 100}%)`;
        dotEls.forEach((d, i) => d.classList.toggle('active', i === index));
    }
    function next() { index = (index + 1) % count; update(); }
    function goTo(i) { index = i % count; update(); restart(); }

    let timer = setInterval(next, intervalMs);
    function restart() { clearInterval(timer); timer = setInterval(next, intervalMs); }

    // Glassflow navbar toggle
    (function () {
        const nav = document.querySelector('.nav');
        if (!nav) return;

        function applyGlass() {
            if (window.scrollY > 16) {
                nav.classList.add('nav--glass');
            } else {
                nav.classList.remove('nav--glass');
            }
        }

        window.addEventListener('scroll', applyGlass, { passive: true });
        applyGlass(); // set initial state on load
    })();

    slider.addEventListener('mouseenter', () => clearInterval(timer));
    slider.addEventListener('mouseleave', restart);

    update();
})();
