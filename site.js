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

  /* ---- Feuille de route : filtres et étapes repliables ---- */
  var filterBoxes = document.querySelectorAll("[data-filter]");
  if (filterBoxes.length) {
    var stages = document.querySelectorAll("[data-stage]");
    var resultLine = document.getElementById("filter-result");
    var STORE = "sunhavenaccess-roadmap-filters";

    /* Écrit un texte dans les deux langues à la fois. Un compteur recalculé qui ne toucherait que
       la langue affichée redeviendrait faux dès qu'on bascule FR/EN — et il n'y a aucun moyen de
       s'en apercevoir, puisque le chiffre reste plausible. */
    function setBilingual(el, fr, en) {
      if (!el) return;
      var frSpan = el.querySelector('[data-lang="fr"]');
      var enSpan = el.querySelector('[data-lang="en"]');
      if (frSpan) frSpan.textContent = fr;
      if (enSpan) enSpan.textContent = en;
    }

    function activeStatuses() {
      var set = {};
      filterBoxes.forEach(function (box) { set[box.getAttribute("data-filter")] = box.checked; });
      return set;
    }

    function apply() {
      var wanted = activeStatuses();
      // Aucun filtre actif : le nombre affiché vaut le total et n'apprend rien. On rend alors la
      // progression, qui est l'information qu'on vient chercher sur cette page.
      var filtering = false;
      filterBoxes.forEach(function (box) { if (!box.checked) filtering = true; });
      var shown = 0;

      stages.forEach(function (stage) {
        var visibleHere = 0;
        stage.querySelectorAll("[data-status]").forEach(function (item) {
          var keep = wanted[item.getAttribute("data-status")] === true;
          item.hidden = !keep;
          if (keep) visibleHere++;
        });

        // Une étape dont plus aucun point ne correspond n'a plus rien à montrer : la laisser
        // ferait un intitulé qu'on ouvre pour trouver le vide.
        stage.hidden = visibleHere === 0;
        shown += visibleHere;

        var label = stage.querySelector("[data-stage-count]");
        if (filtering) {
          setBilingual(label,
            visibleHere + (visibleHere > 1 ? " points affichés" : " point affiché"),
            visibleHere + (visibleHere > 1 ? " items shown" : " item shown"));
        } else {
          var done = stage.getAttribute("data-done");
          var total = stage.getAttribute("data-total");
          setBilingual(label, done + " sur " + total + " au point", done + " of " + total + " solid");
        }
      });

      setBilingual(resultLine,
        shown + (shown > 1 ? " points affichés." : " point affiché."),
        shown + (shown > 1 ? " items shown." : " item shown."));

      save();
    }

    function save() {
      try {
        var open = [];
        stages.forEach(function (s) { if (s.open) open.push(s.id); });
        localStorage.setItem(STORE, JSON.stringify({ statuses: activeStatuses(), open: open }));
      } catch (e) {}
    }

    function restore() {
      var saved = null;
      try { saved = JSON.parse(localStorage.getItem(STORE) || "null"); } catch (e) {}
      if (!saved) return;

      if (saved.statuses) {
        filterBoxes.forEach(function (box) {
          var v = saved.statuses[box.getAttribute("data-filter")];
          if (typeof v === "boolean") box.checked = v;
        });
      }
      if (Array.isArray(saved.open)) {
        stages.forEach(function (s) { s.open = saved.open.indexOf(s.id) !== -1; });
      }
    }

    // `role="status"` sur la ligne de résultat suffit à faire annoncer chaque changement par le
    // lecteur d'écran : elle est vide au départ pour ne rien annoncer à l'arrivée sur la page.
    resultLine && resultLine.setAttribute("aria-live", "polite");

    filterBoxes.forEach(function (box) { box.addEventListener("change", apply); });
    stages.forEach(function (s) { s.addEventListener("toggle", save); });

    var remaining = document.getElementById("filter-remaining");
    if (remaining) {
      remaining.addEventListener("click", function () {
        filterBoxes.forEach(function (box) {
          box.checked = box.getAttribute("data-filter") !== "ok";
        });
        apply();
        stages.forEach(function (s) { if (!s.hidden) s.open = true; });
        save();
      });
    }

    var expand = document.getElementById("expand-all");
    if (expand) {
      expand.addEventListener("click", function () {
        stages.forEach(function (s) { if (!s.hidden) s.open = true; });
        save();
      });
    }

    var collapse = document.getElementById("collapse-all");
    if (collapse) {
      collapse.addEventListener("click", function () {
        stages.forEach(function (s) { s.open = false; });
        save();
      });
    }

    /* Une ancre qui pointe une étape repliée n'affiche rien : le navigateur y saute, et on tombe
       sur un intitulé fermé. On l'ouvre donc soi-même, au chargement et à chaque changement. */
    function openTargetStage() {
      var id = (location.hash || "").replace("#", "");
      if (!id) return;
      var target = document.getElementById(id);
      if (target && target.hasAttribute("data-stage")) target.open = true;
    }
    window.addEventListener("hashchange", openTargetStage);

    restore();
    apply();
    openTargetStage();
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
