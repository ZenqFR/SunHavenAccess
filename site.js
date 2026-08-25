(function () {
  "use strict";

  /* ---- Langue FR/EN ---- */
  function applyLang(lang) {
    document.documentElement.lang = lang;
    document.querySelectorAll('[data-lang="fr"]').forEach(function (el) { el.hidden = lang !== "fr"; });
    document.querySelectorAll('[data-lang="en"]').forEach(function (el) { el.hidden = lang !== "en"; });
    var toggle = document.getElementById("lang-toggle");
    if (toggle) toggle.textContent = lang === "fr" ? "English" : "Français";
    // aria-label ne peut pas contenir de spans data-lang : mis à jour ici pour rester cohérent
    // avec le reste du texte de la page quand on change de langue.
    var toTopBtn = document.getElementById("to-top");
    if (toTopBtn) toTopBtn.setAttribute("aria-label", lang === "fr" ? "Retour en haut de la page" : "Back to top");
  }
  var savedLang = "fr";
  try { savedLang = localStorage.getItem("sunhavenaccess-lang") || "fr"; } catch (e) {}
  applyLang(savedLang);
  var toggleBtn = document.getElementById("lang-toggle");
  if (toggleBtn) {
    toggleBtn.addEventListener("click", function () {
      var next = document.documentElement.lang === "en" ? "fr" : "en";
      applyLang(next);
      try { localStorage.setItem("sunhavenaccess-lang", next); } catch (e) {}
    });
  }

  /* ---- Apparition des cartes au défilement ---- */
  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  var revealEls = document.querySelectorAll(".reveal");
  if (reduceMotion || !("IntersectionObserver" in window)) {
    revealEls.forEach(function (el) { el.classList.add("is-visible"); });
  } else {
    // N'active le masquage CSS (html.js-reveal-active) qu'ici, une fois sûr de pouvoir
    // effectivement révéler les cartes ensuite — sinon elles restent visibles par défaut.
    document.documentElement.classList.add("js-reveal-active");
    var revealObserver = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          revealObserver.unobserve(entry.target);
        }
      });
    }, { threshold: 0.1, rootMargin: "0px 0px -40px 0px" });
    revealEls.forEach(function (el) { revealObserver.observe(el); });
  }

  /* ---- Scrollspy de la nav rapide ---- */
  var navLinks = document.querySelectorAll(".quick-nav a");
  var sections = Array.prototype.map.call(navLinks, function (link) {
    return document.getElementById(link.getAttribute("data-section"));
  }).filter(Boolean);
  if (sections.length && "IntersectionObserver" in window) {
    var setActive = function (id) {
      navLinks.forEach(function (link) {
        var match = link.getAttribute("data-section") === id;
        if (match) link.setAttribute("aria-current", "true");
        else link.removeAttribute("aria-current");
      });
    };
    var navObserver = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) setActive(entry.target.id);
      });
    }, { rootMargin: "-40% 0px -55% 0px" });
    sections.forEach(function (section) { navObserver.observe(section); });
  }

  /* ---- Bouton retour en haut ---- */
  var toTop = document.getElementById("to-top");
  if (toTop) {
    var toggleToTop = function () {
      toTop.classList.toggle("is-visible", window.scrollY > 500);
    };
    window.addEventListener("scroll", toggleToTop, { passive: true });
    toggleToTop();
    toTop.addEventListener("click", function () {
      window.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" });
    });
  }
})();
