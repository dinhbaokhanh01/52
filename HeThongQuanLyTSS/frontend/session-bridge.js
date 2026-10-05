(function(){
  const nativeFetch = window.fetch.bind(window);
  window.fetch = (url, options = {}) => {
    if (typeof url === 'string' && url.startsWith('http://localhost:5134/api') && location.port !== '8000') url = url.replace('http://localhost:5134', location.origin);
    return nativeFetch(url, { ...options, credentials: 'include' });
  };
  document.addEventListener('DOMContentLoaded', async () => {
    const api = location.port === '8000' ? 'http://localhost:5134/api' : '/api';
    for (const link of document.querySelectorAll('a[href="dang-nhap.html"]')) {
      if (link.textContent.includes('Đăng xuất')) link.addEventListener('click', async e => {
        e.preventDefault();
        await fetch(api+'/auth/logout',{method:'POST'});
        localStorage.removeItem('career-portal-intern-profile-id');
        sessionStorage.removeItem('career-portal-intern-profile-id');
        location.assign('dang-nhap.html');
      });
    }
    const nav = document.querySelector('.nav-list');
    const legacy = document.querySelector('main.content');
    if (!nav || !legacy) return;
    try {
      const response = await fetch(api+'/auth/me');
      if (!response.ok) { location.replace('dang-nhap.html'); return; }
      const user = await response.json();
      const expected = user.role === 'Intern' ? 'thuc-tap-sinh.html' : user.role === 'Mentor' ? 'mentor.html' : 'hr.html';
      if (!location.pathname.endsWith('/'+expected)) { location.replace(expected); return; }
      const accountName = document.querySelector('.account-name');
      const accountRole = document.querySelector('.account-role');
      if(accountName) accountName.textContent = user.name;
      if(accountRole) accountRole.textContent = user.role;
      const style = document.createElement('link');
      style.rel = 'stylesheet'; style.href = 'integrated.css'; document.head.append(style);
      const fragment = await fetch('workflow-fragment.html');
      if(!fragment.ok) throw Error('Không tải được giao diện nghiệp vụ.');
      const host = document.createElement('section');
      host.id = 'integrated-portal'; host.className = 'content'; host.hidden = true;
      host.innerHTML = await fragment.text(); legacy.after(host);
      const originalButtons = [...nav.querySelectorAll('[data-view]')];
      let activeModule = null;
      for(const item of originalButtons) item.addEventListener('click', () => {
        host.hidden = true; legacy.hidden = false;
        for(const b of nav.querySelectorAll('[data-module]'))b.removeAttribute('aria-current');
        activeModule = null;
      });
      document.addEventListener('portal-ready', event => {
        const moduleNav = host.querySelector('#nav');
        const moduleButtons = [...moduleNav.querySelectorAll('button')];
        moduleButtons.forEach((source, index) => {
          const key = event.detail.allowed[index];
          const b = document.createElement('button'); b.type='button'; b.className='nav-item'; b.dataset.module=key;
          const label = document.createElement('span'); label.className='nav-item-text';label.textContent=source.textContent;b.append(label);
          b.addEventListener('click',async()=>{
            legacy.hidden = true;host.hidden = false;
            for(const item of nav.querySelectorAll('.nav-item'))item.removeAttribute('aria-current');
            b.setAttribute('aria-current','page');activeModule=key;
            const crumb=document.getElementById('breadcrumb-current')||document.getElementById('breadcrumb-title');if(crumb)crumb.textContent=source.textContent;
            try{await event.detail.show(key);}catch(error){host.querySelector('#message').textContent=error.message;}
          });nav.append(b);
        });
        if(user.role === 'Mentor') {
          originalButtons.forEach(b=>b.remove());
          nav.querySelector('[data-module]')?.click();
        }
      }, {once:true});
      const script = document.createElement('script');script.src='portal.js';document.body.append(script);
    }catch(error){
      const message=document.createElement('p');message.setAttribute('role','alert');message.textContent='Không tải được chức năng bổ sung: '+error.message;legacy.prepend(message);
    }
  });
})();
