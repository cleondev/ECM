import './profile.css';
import {
  ApiError,
  getProfile,
  updateProfile,
  type UpdateProfilePayload,
  type UserProfile,
} from '../services/profileApi';

function createMetaBadge(text: string): HTMLSpanElement {
  const badge = document.createElement('span');
  badge.textContent = text;
  return badge;
}

function createRoleBadge(text: string): HTMLSpanElement {
  const badge = document.createElement('span');
  badge.className = 'profile-role';
  badge.textContent = text;
  return badge;
}

export function renderProfilePage(container: HTMLElement): void {
  container.innerHTML = '';

  const shell = document.createElement('section');
  shell.className = 'profile-shell';
  container.appendChild(shell);

  const header = document.createElement('header');
  header.className = 'profile-header';
  header.innerHTML = `
    <h1>Hồ sơ cá nhân</h1>
    <p>Thông tin dùng để hiển thị trong hệ thống ECM và các quy trình liên quan.</p>
  `;
  shell.appendChild(header);

  const form = document.createElement('form');
  form.className = 'profile-card';
  shell.appendChild(form);

  const emailLabel = document.createElement('label');
  emailLabel.textContent = 'Email';
  form.appendChild(emailLabel);

  const emailValue = document.createElement('div');
  emailValue.className = 'readonly';
  form.appendChild(emailValue);

  const nameLabel = document.createElement('label');
  nameLabel.htmlFor = 'profile-display-name';
  nameLabel.textContent = 'Họ và tên hiển thị';
  form.appendChild(nameLabel);

  const nameInput = document.createElement('input');
  nameInput.id = 'profile-display-name';
  nameInput.name = 'displayName';
  nameInput.placeholder = 'Ví dụ: Nguyễn Văn A';
  nameInput.autocomplete = 'name';
  form.appendChild(nameInput);

  const departmentLabel = document.createElement('label');
  departmentLabel.htmlFor = 'profile-department';
  departmentLabel.textContent = 'Phòng ban';
  form.appendChild(departmentLabel);

  const departmentInput = document.createElement('input');
  departmentInput.id = 'profile-department';
  departmentInput.name = 'department';
  departmentInput.placeholder = 'Ví dụ: Phòng CNTT';
  departmentInput.autocomplete = 'organization';
  form.appendChild(departmentInput);

  const actions = document.createElement('div');
  actions.className = 'profile-actions';
  form.appendChild(actions);

  const saveButton = document.createElement('button');
  saveButton.type = 'submit';
  saveButton.textContent = 'Lưu thay đổi';
  actions.appendChild(saveButton);

  const status = document.createElement('div');
  status.className = 'profile-status';
  actions.appendChild(status);

  const meta = document.createElement('div');
  meta.className = 'profile-meta';
  shell.appendChild(meta);

  const roles = document.createElement('div');
  roles.className = 'profile-roles';
  shell.appendChild(roles);

  const emptyState = document.createElement('div');
  emptyState.className = 'profile-empty';
  emptyState.textContent = 'Không tìm thấy hồ sơ người dùng.';
  emptyState.style.display = 'none';
  shell.appendChild(emptyState);

  let currentProfile: UserProfile | null = null;

  function setStatus(message: string, isError = false): void {
    status.textContent = message;
    status.classList.toggle('error', isError);
  }

  function setFormDisabled(disabled: boolean): void {
    nameInput.disabled = disabled;
    departmentInput.disabled = disabled;
    saveButton.disabled = disabled;
  }

  function showProfile(profile: UserProfile): void {
    emptyState.style.display = 'none';
    form.style.display = 'grid';
    meta.style.display = 'flex';
    roles.style.display = 'flex';

    emailValue.textContent = profile.email;
    nameInput.value = profile.displayName;
    departmentInput.value = profile.department ?? '';

    meta.innerHTML = '';
    meta.append(
      createMetaBadge(profile.isActive ? 'Đang hoạt động' : 'Đã vô hiệu hóa'),
      createMetaBadge(`Tạo ngày ${new Date(profile.createdAtUtc).toLocaleString('vi-VN')}`)
    );

    roles.innerHTML = '';
    if (profile.roles.length === 0) {
      roles.appendChild(createRoleBadge('Chưa có vai trò'));
    } else {
      for (const role of profile.roles) {
        roles.appendChild(createRoleBadge(role.name));
      }
    }
  }

  function showEmpty(): void {
    currentProfile = null;
    form.style.display = 'none';
    meta.style.display = 'none';
    roles.style.display = 'none';
    emptyState.style.display = 'block';
  }

  async function loadProfile(): Promise<void> {
    setStatus('Đang tải hồ sơ…');
    setFormDisabled(true);
    try {
      const profile = await getProfile();
      if (!profile) {
        showEmpty();
        setStatus('Không tìm thấy hồ sơ người dùng.', true);
        return;
      }

      currentProfile = profile;
      showProfile(profile);
      setStatus('');
      setFormDisabled(false);
    } catch (error) {
      console.error(error);
      showEmpty();
      setStatus('Không thể tải hồ sơ. Vui lòng thử lại sau.', true);
    }
  }

  async function handleSubmit(event: SubmitEvent): Promise<void> {
    event.preventDefault();

    if (!currentProfile) {
      setStatus('Không tìm thấy hồ sơ người dùng.', true);
      return;
    }

    const payload: UpdateProfilePayload = {
      displayName: nameInput.value.trim(),
      department: departmentInput.value.trim() || null,
    };

    setStatus('Đang lưu thay đổi…');
    setFormDisabled(true);
    saveButton.textContent = 'Đang lưu…';

    try {
      const updated = await updateProfile(payload);
      if (!updated) {
        showEmpty();
        setStatus('Hồ sơ không còn tồn tại.', true);
        return;
      }

      currentProfile = updated;
      showProfile(updated);
      setStatus('Đã cập nhật hồ sơ.', false);
    } catch (error) {
      console.error(error);
      if (error instanceof ApiError) {
        if (error.status === 404) {
          showEmpty();
          setStatus('Hồ sơ không còn tồn tại.', true);
        } else if (error.details) {
          const messages = Object.values(error.details).flat();
          setStatus(messages.join('\n'), true);
        } else {
          setStatus(error.message, true);
        }
      } else {
        setStatus('Không thể cập nhật hồ sơ. Vui lòng thử lại sau.', true);
      }
    } finally {
      saveButton.textContent = 'Lưu thay đổi';
      if (currentProfile) {
        setFormDisabled(false);
      }
    }
  }

  form.addEventListener('submit', handleSubmit);

  void loadProfile();
}
