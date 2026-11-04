/* RentAll — menu original com suporte a teclado, estado acessível e fecho fora. */
class MobileNavbar {
  constructor(mobileMenu, navList, navLinks) {
    this.mobileMenu = document.querySelector(mobileMenu);
    this.navList = document.querySelector(navList);
    this.navLinks = document.querySelectorAll(navLinks);
    this.activeClass = "active";
    this.handleClick = this.handleClick.bind(this);
    this.close = this.close.bind(this);
  }

  setOpen(open) {
    if (!this.mobileMenu || !this.navList) return;
    this.navList.classList.toggle(this.activeClass, open);
    this.mobileMenu.classList.toggle(this.activeClass, open);
    this.mobileMenu.setAttribute("aria-expanded", String(open));
    this.mobileMenu.setAttribute("aria-label", open ? "Fechar menu" : "Abrir menu");
    this.navLinks.forEach(link => { link.style.animation = ""; });
  }

  handleClick() {
    this.setOpen(!this.navList.classList.contains(this.activeClass));
  }

  close() { this.setOpen(false); }

  init() {
    if (!this.mobileMenu || !this.navList) return this;
    if (this.mobileMenu.dataset.raNavBound === "true") return this;
    this.mobileMenu.dataset.raNavBound = "true";
    this.mobileMenu.addEventListener("click", this.handleClick);
    this.navList.addEventListener("click", event => {
      if (event.target.closest("a")) this.close();
    });
    document.addEventListener("keydown", event => {
      if (event.key === "Escape" && this.navList.classList.contains(this.activeClass)) {
        this.close();
        this.mobileMenu.focus();
      }
    });
    document.addEventListener("click", event => {
      if (!this.mobileMenu.contains(event.target) && !this.navList.contains(event.target)) {
        this.close();
      }
    });
    window.addEventListener("resize", () => { if (window.innerWidth > 999) this.close(); });
    return this;
  }
}
new MobileNavbar(".mobile-menu", ".nav-list", ".nav-list li").init();
