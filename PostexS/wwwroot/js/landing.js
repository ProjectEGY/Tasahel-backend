// ======== AOS Initialize ========
AOS.init({
    duration: 800,
    once: true,
    offset: 100
});

// ======== Navbar Scroll Effect ========
window.addEventListener('scroll', function () {
    var nav = document.getElementById('mainNav');
    if (nav) {
        if (window.scrollY > 50) {
            nav.classList.add('scrolled');
        } else {
            nav.classList.remove('scrolled');
        }
    }
});

// ======== Smooth Scroll ========
document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        var target = document.querySelector(this.getAttribute('href'));
        if (target) {
            var offset = 70;
            var targetPos = target.getBoundingClientRect().top + window.pageYOffset - offset;
            window.scrollTo({ top: targetPos, behavior: 'smooth' });

            // Close mobile menu
            var collapse = document.querySelector('.navbar-collapse');
            if (collapse && collapse.classList.contains('show')) {
                var bsCollapse = bootstrap.Collapse.getInstance(collapse);
                if (bsCollapse) bsCollapse.hide();
            }
        }
    });
});

// ======== Counter Animation ========
function animateCounters() {
    var counters = document.querySelectorAll('.stat-number');
    counters.forEach(function (counter) {
        if (counter.dataset.animated) return;

        var target = counter.getAttribute('data-target');
        if (!target) return;

        // Check if target contains non-numeric chars (like +, %, etc.)
        var suffix = target.replace(/[\d,.]/g, '');
        var numericVal = parseFloat(target.replace(/[^\d.]/g, ''));

        if (isNaN(numericVal)) {
            counter.textContent = target;
            return;
        }

        var duration = 2000;
        var startTime = null;

        function step(timestamp) {
            if (!startTime) startTime = timestamp;
            var progress = Math.min((timestamp - startTime) / duration, 1);
            var ease = 1 - Math.pow(1 - progress, 3); // easeOutCubic
            var current = Math.floor(ease * numericVal);
            counter.textContent = current.toLocaleString() + suffix;
            if (progress < 1) {
                requestAnimationFrame(step);
            } else {
                counter.textContent = target;
            }
        }

        counter.dataset.animated = 'true';
        requestAnimationFrame(step);
    });
}

// ======== Intersection Observer for Counters ========
var statsSection = document.getElementById('stats');
if (statsSection) {
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                animateCounters();
            }
        });
    }, { threshold: 0.3 });
    observer.observe(statsSection);
}

// ======== Active Nav Link on Scroll ========
window.addEventListener('scroll', function () {
    var sections = document.querySelectorAll('section[id]');
    var scrollPos = window.scrollY + 100;

    sections.forEach(function (section) {
        var top = section.offsetTop;
        var height = section.offsetHeight;
        var id = section.getAttribute('id');
        var link = document.querySelector('.nav-link[href="#' + id + '"]');

        if (link) {
            if (scrollPos >= top && scrollPos < top + height) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        }
    });
});
