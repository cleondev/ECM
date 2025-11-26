import { useState } from 'react';
import { Send, FileText } from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Textarea } from '../ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Separator } from '../ui/separator';

export function FormTab() {
  const [formData, setFormData] = useState({
    title: '',
    category: '',
    priority: '',
    description: ''
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    console.log('Form submitted:', formData);
  };

  return (
    <div className="p-4">
      <div className="space-y-4">
        <div className="flex items-center gap-2">
          <FileText className="h-4 w-4 text-neutral-500" />
          <h3 className="text-sm">Thông tin bổ sung</h3>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="title" className="text-xs">Tiêu đề</Label>
            <Input
              id="title"
              placeholder="Nhập tiêu đề..."
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              className="h-8 text-sm"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="category" className="text-xs">Danh mục</Label>
            <Select 
              value={formData.category}
              onValueChange={(value) => setFormData({ ...formData, category: value })}
            >
              <SelectTrigger id="category" className="h-8 text-sm">
                <SelectValue placeholder="Chọn danh mục" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="design">Thiết kế</SelectItem>
                <SelectItem value="development">Phát triển</SelectItem>
                <SelectItem value="marketing">Marketing</SelectItem>
                <SelectItem value="other">Khác</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="priority" className="text-xs">Độ ưu tiên</Label>
            <Select
              value={formData.priority}
              onValueChange={(value) => setFormData({ ...formData, priority: value })}
            >
              <SelectTrigger id="priority" className="h-8 text-sm">
                <SelectValue placeholder="Chọn độ ưu tiên" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="high">Cao</SelectItem>
                <SelectItem value="medium">Trung bình</SelectItem>
                <SelectItem value="low">Thấp</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="description" className="text-xs">Mô tả</Label>
            <Textarea
              id="description"
              placeholder="Nhập mô tả chi tiết..."
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              className="min-h-[100px] text-sm resize-none"
            />
          </div>

          <Separator />

          <Button type="submit" size="sm" className="w-full text-xs">
            <Send className="h-3 w-3 mr-1" />
            Gửi thông tin
          </Button>
        </form>
      </div>
    </div>
  );
}
