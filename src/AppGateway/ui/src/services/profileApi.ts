export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  department: string | null;
  isActive: boolean;
  createdAtUtc: string;
  roles: Array<{ id: string; name: string }>;
}

export interface UpdateProfilePayload {
  displayName: string;
  department: string | null;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  status: number;
  details?: Record<string, string[]>;

  constructor(message: string, status: number, details?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.details = details;
  }
}

async function readJson<T>(response: Response): Promise<T | null> {
  try {
    return (await response.json()) as T;
  } catch (error) {
    console.warn('Không thể phân tích JSON từ phản hồi.', error);
    return null;
  }
}

function normaliseProfile(data: any): UserProfile {
  const sourceRoles = Array.isArray(data?.roles)
    ? data.roles
    : Array.isArray(data?.Roles)
      ? data.Roles
      : [];

  return {
    id: data?.id ?? data?.Id ?? '',
    email: data?.email ?? data?.Email ?? '',
    displayName: data?.displayName ?? data?.DisplayName ?? '',
    department: data?.department ?? data?.Department ?? null,
    isActive: Boolean(data?.isActive ?? data?.IsActive),
    createdAtUtc: data?.createdAtUtc ?? data?.CreatedAtUtc ?? '',
    roles: sourceRoles.map((role: any) => ({
      id: role?.id ?? role?.Id ?? '',
      name: role?.name ?? role?.Name ?? '',
    })),
  };
}

export async function getProfile(): Promise<UserProfile | null> {
  const response = await fetch('/api/gateway/access-control/profile', {
    method: 'GET',
    credentials: 'include',
    headers: {
      Accept: 'application/json',
    },
  });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new ApiError('Không thể tải hồ sơ người dùng.', response.status);
  }

  const data = await readJson<unknown>(response);
  return data ? normaliseProfile(data) : null;
}

export async function updateProfile(payload: UpdateProfilePayload): Promise<UserProfile | null> {
  const response = await fetch('/api/gateway/access-control/profile', {
    method: 'PUT',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify(payload),
  });

  if (response.status === 404) {
    return null;
  }

  if (response.status === 400) {
    const problem = (await readJson<ProblemDetails>(response)) ?? undefined;
    const message = problem?.detail ?? problem?.title ?? 'Thông tin gửi lên không hợp lệ.';
    throw new ApiError(message, response.status, problem?.errors);
  }

  if (!response.ok) {
    throw new ApiError('Không thể cập nhật hồ sơ người dùng.', response.status);
  }

  const data = await readJson<unknown>(response);
  if (!data) {
    throw new ApiError('Phản hồi không hợp lệ từ máy chủ.', response.status);
  }

  return normaliseProfile(data);
}
