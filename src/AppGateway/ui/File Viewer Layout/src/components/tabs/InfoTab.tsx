import { Calendar, User, HardDrive, Clock, Users, Lock, File, Tag, Plus, X } from 'lucide-react';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { Separator } from '../ui/separator';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { useState } from 'react';

interface InfoTabProps {
  file: {
    name: string;
    type: string;
    size: number;
    owner: string;
    createdAt: string;
    modifiedAt: string;
    tags?: Array<{
      name: string;
      color: string;
    }>;
    sharedWith: Array<{
      name: string;
      email: string;
    }>;
  };
}

export function InfoTab({ file }: InfoTabProps) {
  const [tags, setTags] = useState(file.tags || []);
  const [isAddingTag, setIsAddingTag] = useState(false);

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

  const getTagColorClass = (color: string) => {
    const colorMap: Record<string, string> = {
      blue: 'bg-blue-500 hover:bg-blue-600 text-white',
      cyan: 'bg-cyan-500 hover:bg-cyan-600 text-white',
      green: 'bg-green-500 hover:bg-green-600 text-white',
      yellow: 'bg-yellow-500 hover:bg-yellow-600 text-white',
      purple: 'bg-purple-500 hover:bg-purple-600 text-white',
      pink: 'bg-pink-500 hover:bg-pink-600 text-white',
      red: 'bg-red-500 hover:bg-red-600 text-white',
      orange: 'bg-orange-500 hover:bg-orange-600 text-white',
    };
    return colorMap[color] || 'bg-neutral-500 hover:bg-neutral-600 text-white';
  };

  const removeTag = (index: number) => {
    setTags(tags.filter((_, i) => i !== index));
  };

  return (
    <div className="p-4 space-y-6">
      {/* Tags Section - Prominent */}
      <div className="space-y-3 bg-blue-50 dark:bg-blue-950/20 p-3 rounded-lg border border-blue-200 dark:border-blue-800">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Tag className="h-4 w-4 text-blue-600" />
            <h3 className="text-sm">Tags</h3>
          </div>
          <Button 
            variant="ghost" 
            size="sm" 
            className="h-6 px-2 text-xs"
            onClick={() => setIsAddingTag(!isAddingTag)}
          >
            <Plus className="h-3 w-3" />
          </Button>
        </div>

        {tags.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {tags.map((tag, index) => (
              <Badge
                key={index}
                className={`${getTagColorClass(tag.color)} group cursor-pointer transition-all text-xs px-2 py-1`}
              >
                {tag.name}
                <button
                  onClick={() => removeTag(index)}
                  className="ml-1.5 opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  <X className="h-3 w-3" />
                </button>
              </Badge>
            ))}
          </div>
        ) : (
          <p className="text-xs text-neutral-500 italic">Chưa có tag nào</p>
        )}

        {isAddingTag && (
          <div className="flex flex-wrap gap-2 pt-2 border-t border-blue-200 dark:border-blue-800">
            {['blue', 'cyan', 'green', 'yellow', 'purple', 'pink'].map((color) => (
              <Button
                key={color}
                size="sm"
                className={`h-6 text-xs ${getTagColorClass(color)}`}
                onClick={() => {
                  setTags([...tags, { name: `Tag ${tags.length + 1}`, color }]);
                  setIsAddingTag(false);
                }}
              >
                + {color}
              </Button>
            ))}
          </div>
        )}
      </div>

      <Separator />

      {/* File Details */}
      <div className="space-y-3">
        <h3 className="text-sm">Chi tiết file</h3>

        <div className="space-y-3">
          <div className="flex items-start gap-3">
            <File className="h-4 w-4 mt-0.5 text-neutral-500 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <div className="text-xs text-neutral-500">Tên file</div>
              <div className="text-sm break-words">{file.name}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <HardDrive className="h-4 w-4 mt-0.5 text-neutral-500 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <div className="text-xs text-neutral-500">Kích thước</div>
              <div className="text-sm">{formatFileSize(file.size)}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <File className="h-4 w-4 mt-0.5 text-neutral-500 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <div className="text-xs text-neutral-500">Loại file</div>
              <div className="text-sm">{file.type}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <User className="h-4 w-4 mt-0.5 text-neutral-500 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <div className="text-xs text-neutral-500">Chủ sở hữu</div>
              <div className="text-sm">{file.owner}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <Calendar className="h-4 w-4 mt-0.5 text-neutral-500 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <div className="text-xs text-neutral-500">Tạo lúc</div>
              <div className="text-sm">{formatDate(file.createdAt)}</div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <Clock className="h-4 w-4 mt-0.5 text-neutral-500 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <div className="text-xs text-neutral-500">Chỉnh sửa lần cuối</div>
              <div className="text-sm">{formatDate(file.modifiedAt)}</div>
            </div>
          </div>
        </div>
      </div>

      <Separator />

      {/* Shared With */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="text-sm">Chia sẻ với</h3>
          <Badge variant="secondary" className="text-xs">
            <Users className="h-3 w-3 mr-1" />
            {file.sharedWith.length}
          </Badge>
        </div>

        <div className="space-y-3">
          {file.sharedWith.map((user, index) => (
            <div key={index} className="flex items-center gap-2">
              <Avatar className="h-7 w-7">
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
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Lock className="h-4 w-4 text-neutral-500" />
          <h3 className="text-sm">Quyền truy cập</h3>
        </div>
        <div className="text-xs text-neutral-600">
          Chỉ những người được chia sẻ mới có thể xem file này
        </div>
      </div>
    </div>
  );
}