import { 
  Download, 
  Share2, 
  MoreVertical, 
  ZoomIn, 
  ZoomOut, 
  PanelRightClose,
  PanelRightOpen,
  Maximize,
  Star,
  Trash2,
  Users
} from 'lucide-react';
import { Button } from './ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from './ui/dropdown-menu';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from './ui/popover';
import { Separator } from './ui/separator';
import { Avatar, AvatarFallback } from './ui/avatar';
import { Badge } from './ui/badge';

interface ToolbarProps {
  file: {
    name: string;
    type: string;
    activeViewers?: Array<{
      name: string;
      email: string;
      status: string;
      color: string;
    }>;
  };
  onSidebarToggle: () => void;
  isSidebarOpen: boolean;
}

export function Toolbar({ 
  file, 
  onSidebarToggle,
  isSidebarOpen 
}: ToolbarProps) {
  const getInitials = (name: string): string => {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const getAvatarColor = (color: string) => {
    const colorMap: Record<string, string> = {
      blue: 'bg-blue-500',
      green: 'bg-green-500',
      purple: 'bg-purple-500',
      orange: 'bg-orange-500',
      pink: 'bg-pink-500',
      red: 'bg-red-500',
    };
    return colorMap[color] || 'bg-neutral-500';
  };

  const getStatusText = (status: string) => {
    const statusMap: Record<string, string> = {
      active: 'Đang chỉnh sửa',
      viewing: 'Đang xem',
      idle: 'Không hoạt động',
    };
    return statusMap[status] || 'Đang xem';
  };

  const activeViewers = file.activeViewers || [];

  return (
    <div className="h-14 bg-white border-b border-neutral-200 flex items-center justify-between px-4 gap-4">
      {/* Left: File Name & Tags */}
      <div className="flex items-center gap-3 min-w-0 flex-1">
        <div className="min-w-0 flex items-center gap-2">
          <h1 className="truncate text-sm">{file.name}</h1>
        </div>
      </div>

      {/* Center: View Controls */}
      <div className="flex items-center gap-1">
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <ZoomOut className="h-4 w-4" />
        </Button>
        <span className="text-sm text-neutral-600 min-w-[50px] text-center">100%</span>
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <ZoomIn className="h-4 w-4" />
        </Button>
        <Separator orientation="vertical" className="h-6 mx-2" />
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <Maximize className="h-4 w-4" />
        </Button>
      </div>

      {/* Right: Actions */}
      <div className="flex items-center gap-1">
        {/* Active Viewers */}
        {activeViewers.length > 0 && (
          <>
            <Popover>
              <PopoverTrigger asChild>
                <Button variant="ghost" className="h-8 px-2 gap-1">
                  <div className="flex -space-x-2">
                    {activeViewers.slice(0, 3).map((viewer, index) => (
                      <Avatar 
                        key={index} 
                        className={`h-6 w-6 border-2 border-white ${getAvatarColor(viewer.color)}`}
                      >
                        <AvatarFallback className="text-xs text-white">
                          {getInitials(viewer.name)}
                        </AvatarFallback>
                      </Avatar>
                    ))}
                  </div>
                  {activeViewers.length > 3 && (
                    <span className="text-xs text-neutral-600">+{activeViewers.length - 3}</span>
                  )}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-64 p-3" align="end">
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <h4 className="text-sm">Đang xem ({activeViewers.length})</h4>
                    <Users className="h-4 w-4 text-neutral-500" />
                  </div>
                  <Separator />
                  <div className="space-y-2">
                    {activeViewers.map((viewer, index) => (
                      <div key={index} className="flex items-center gap-2">
                        <Avatar className={`h-7 w-7 ${getAvatarColor(viewer.color)}`}>
                          <AvatarFallback className="text-xs text-white">
                            {getInitials(viewer.name)}
                          </AvatarFallback>
                        </Avatar>
                        <div className="flex-1 min-w-0">
                          <div className="text-sm truncate">{viewer.name}</div>
                          <div className="text-xs text-neutral-500">{getStatusText(viewer.status)}</div>
                        </div>
                        <div className={`h-2 w-2 rounded-full ${viewer.status === 'active' ? 'bg-green-500' : 'bg-neutral-300'}`} />
                      </div>
                    ))}
                  </div>
                </div>
              </PopoverContent>
            </Popover>
            <Separator orientation="vertical" className="h-6 mx-1" />
          </>
        )}

        <Button variant="ghost" size="icon" className="h-8 w-8">
          <Star className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <Download className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <Share2 className="h-4 w-4" />
        </Button>
        
        <Separator orientation="vertical" className="h-6 mx-2" />
        
        <Button 
          variant={isSidebarOpen ? "secondary" : "ghost"} 
          size="icon"
          className="h-8 w-8"
          onClick={onSidebarToggle}
        >
          {isSidebarOpen ? (
            <PanelRightClose className="h-4 w-4" />
          ) : (
            <PanelRightOpen className="h-4 w-4" />
          )}
        </Button>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <MoreVertical className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem>
              <Download className="h-4 w-4 mr-2" />
              Tải xuống
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Share2 className="h-4 w-4 mr-2" />
              Chia sẻ
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem>
              Đổi tên
            </DropdownMenuItem>
            <DropdownMenuItem>
              Di chuyển
            </DropdownMenuItem>
            <DropdownMenuItem>
              Tạo bản sao
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem className="text-red-600">
              <Trash2 className="h-4 w-4 mr-2" />
              Xóa
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  );
}