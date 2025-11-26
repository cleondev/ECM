import { Calendar, User, HardDrive, Clock, Users, Lock } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from './ui/avatar';
import { Separator } from './ui/separator';
import { Badge } from './ui/badge';
import { ImageWithFallback } from './figma/ImageWithFallback';

interface FileMetadataProps {
  file: {
    name: string;
    type: string;
    size: number;
    owner: string;
    createdAt: string;
    modifiedAt: string;
    sharedWith: Array<{
      name: string;
      email: string;
    }>;
  };
}

export function FileMetadata({ file }: FileMetadataProps) {
  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    return (bytes / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getInitials = (name: string): string => {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  return (
    <div className="p-6 space-y-6">
      {/* File Preview Thumbnail */}
      <div className="aspect-video bg-neutral-100 rounded-lg overflow-hidden">
        <ImageWithFallback
          src={file.type.startsWith('image/') ? 'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=400' : ''}
          alt={file.name}
          className="w-full h-full object-cover"
        />
      </div>

      {/* File Name */}
      <div>
        <h2 className="break-words">{file.name}</h2>
      </div>

      <Separator />

      {/* File Details */}
      <div className="space-y-4">
        <h3>Chi tiết file</h3>

        <div className="space-y-3">
          <div className="flex items-start gap-3">
            <HardDrive className="h-4 w-4 mt-0.5 text-neutral-500" />
            <div className="flex-1 min-w-0">
              <div className="text-sm text-neutral-500">Kích thước</div>
              <div className="text-sm">{formatFileSize(file.size)}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <FileType className="h-4 w-4 mt-0.5 text-neutral-500" />
            <div className="flex-1 min-w-0">
              <div className="text-sm text-neutral-500">Loại file</div>
              <div className="text-sm">{file.type}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <User className="h-4 w-4 mt-0.5 text-neutral-500" />
            <div className="flex-1 min-w-0">
              <div className="text-sm text-neutral-500">Chủ sở hữu</div>
              <div className="text-sm">{file.owner}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <Calendar className="h-4 w-4 mt-0.5 text-neutral-500" />
            <div className="flex-1 min-w-0">
              <div className="text-sm text-neutral-500">Tạo lúc</div>
              <div className="text-sm">{formatDate(file.createdAt)}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <Clock className="h-4 w-4 mt-0.5 text-neutral-500" />
            <div className="flex-1 min-w-0">
              <div className="text-sm text-neutral-500">Chỉnh sửa lần cuối</div>
              <div className="text-sm">{formatDate(file.modifiedAt)}</div>
            </div>
          </div>
        </div>
      </div>

      <Separator />

      {/* Shared With */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3>Chia sẻ với</h3>
          <Badge variant="secondary">
            <Users className="h-3 w-3 mr-1" />
            {file.sharedWith.length}
          </Badge>
        </div>

        <div className="space-y-3">
          {file.sharedWith.map((user, index) => (
            <div key={index} className="flex items-center gap-3">
              <Avatar className="h-8 w-8">
                <AvatarFallback className="text-xs">
                  {getInitials(user.name)}
                </AvatarFallback>
              </Avatar>
              <div className="flex-1 min-w-0">
                <div className="text-sm truncate">{user.name}</div>
                <div className="text-xs text-neutral-500 truncate">{user.email}</div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <Separator />

      {/* Permissions */}
      <div className="space-y-3">
        <div className="flex items-center gap-2">
          <Lock className="h-4 w-4 text-neutral-500" />
          <h3>Quyền truy cập</h3>
        </div>
        <div className="text-sm text-neutral-600">
          Chỉ những người được chia sẻ mới có thể xem file này
        </div>
      </div>
    </div>
  );
}

function FileType({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <polyline points="14 2 14 8 20 8" />
    </svg>
  );
}