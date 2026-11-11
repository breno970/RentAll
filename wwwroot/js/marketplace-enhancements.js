(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const register = document.querySelector("[data-registration-form]");
    if (register) {
      const terms = register.querySelector('input[name="AcceptTerms"]');
      const submit = register.querySelector('button[type="submit"]');
      const password = register.querySelector("#Input_Password");
      if (password) {
        const meter = document.createElement("div");
        meter.className = "ra-password-meter";
        meter.setAttribute("aria-hidden","true");
        for (let i=0;i<4;i++) meter.appendChild(document.createElement("span"));
        const hint = document.createElement("small");
        hint.className="ra-password-hint"; hint.id="ra-password-hint";
        hint.textContent="Escolhe uma palavra-passe forte.";
        const host = password.closest(".password-field");
        host?.after(meter, hint);
        password.addEventListener("input", () => {
          const v = password.value;
          const rules = [v.length >= 8, /[a-z]/.test(v) && /[A-Z]/.test(v), /\d/.test(v), /[^a-z\d]/i.test(v)];
          const n=rules.filter(Boolean).length;
          meter.dataset.strength=String(n);
          hint.textContent=n===4 ? "Cumpre os requisitos de segurança." : "Usa pelo menos 8 caracteres, maiúsculas, minúsculas, um número e um símbolo.";
        });
      }
      terms?.addEventListener("change", () => {
        terms.setCustomValidity("");
        const serverMessage = register.querySelector('[data-valmsg-for="AcceptTerms"]');
        if (terms.checked && serverMessage) serverMessage.textContent = "";
      });
      register.addEventListener("submit", event => {
        if (terms && !terms.checked) {
          event.preventDefault();
          terms.setCustomValidity("É necessário aceitar os termos de utilização.");
          terms.reportValidity();
          return;
        }
        if (!register.checkValidity()) return;
        submit?.setAttribute("aria-busy","true");
      });
    }

    const progressForm = document.querySelector("[data-single-publish]");
    if (progressForm) {
      const section = document.querySelector(".ra-publish-head");
      if (section) {
        const wrap=document.createElement("div"); wrap.className="ra-progress-track";
        const fill=document.createElement("span"); fill.className="ra-progress-fill"; wrap.appendChild(fill);
        section.appendChild(wrap);
        const relevant=["Nome","Descricao","Categoria","Tipo","Pais","Estado","Cidade"];
        const refresh=()=>{
          let completed=0;
          for(const name of relevant){const el=progressForm.elements.namedItem(name);if(el && String(el.value || "").trim()) completed++;}
          const input=progressForm.querySelector("[data-photo-files]");
          if(input?.files?.length) completed++;
          fill.style.width=Math.round(completed/(relevant.length+1)*100)+"%";
          wrap.setAttribute("aria-label", "Preenchimento do anúncio: "+Math.round(completed/(relevant.length+1)*100)+"%");
        };
        progressForm.addEventListener("input",refresh);
        progressForm.addEventListener("change",refresh);
        refresh();
      }
    }
    const topButton=document.createElement("button");
    topButton.type="button";
    topButton.className="ra-scroll-top";
    topButton.setAttribute("aria-label","Voltar ao topo");
    topButton.innerHTML='<i class="fas fa-arrow-up" aria-hidden="true"></i>';
    document.body.appendChild(topButton);
    const update=()=>topButton.classList.toggle("is-visible",window.scrollY>700);
    window.addEventListener("scroll", update, { passive:true });
    topButton.addEventListener("click",()=>window.scrollTo({top:0,behavior:matchMedia("(prefers-reduced-motion: reduce)").matches?"instant":"smooth"}));
    update();
  });
})();