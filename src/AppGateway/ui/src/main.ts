import { renderProfilePage } from './app/profile';

document.addEventListener('DOMContentLoaded', () => {
  const container = document.getElementById('app');
  if (!container) {
    throw new Error('Missing application root element.');
  }

  renderProfilePage(container);
});
