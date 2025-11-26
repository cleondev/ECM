import { useState } from 'react';
import { Send, Smile, Paperclip, MoreVertical, X } from 'lucide-react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Avatar, AvatarFallback, AvatarImage } from './ui/avatar';
import { ScrollArea } from './ui/scroll-area';
import { Separator } from './ui/separator';

interface ChatPanelProps {
  file: {
    name: string;
  };
}

interface Message {
  id: string;
  sender: string;
  content: string;
  timestamp: string;
  isCurrentUser: boolean;
}

const mockMessages: Message[] = [
  {
    id: '1',
    sender: 'Tran Thi B',
    content: 'Chào mọi người! Mình vừa tải file này lên đây.',
    timestamp: '10:30',
    isCurrentUser: false
  },
  {
    id: '2',
    sender: 'Bạn',
    content: 'Cảm ơn! Hình ảnh rất đẹp nhé.',
    timestamp: '10:32',
    isCurrentUser: true
  },
  {
    id: '3',
    sender: 'Le Van C',
    content: 'Mình có thể tải xuống để sử dụng cho dự án không?',
    timestamp: '10:35',
    isCurrentUser: false
  },
  {
    id: '4',
    sender: 'Bạn',
    content: 'Được chứ, mọi người cứ tự nhiên nhé!',
    timestamp: '10:36',
    isCurrentUser: true
  }
];

export function ChatPanel({ file }: ChatPanelProps) {
  const [messages, setMessages] = useState<Message[]>(mockMessages);
  const [newMessage, setNewMessage] = useState('');

  const handleSendMessage = () => {
    if (newMessage.trim()) {
      const message: Message = {
        id: Date.now().toString(),
        sender: 'Bạn',
        content: newMessage,
        timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
        isCurrentUser: true
      };
      setMessages([...messages, message]);
      setNewMessage('');
    }
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
    <div className="h-full flex flex-col">
      {/* Chat Header */}
      <div className="p-4 border-b border-neutral-200">
        <div className="flex items-center justify-between">
          <div>
            <h3>Thảo luận</h3>
            <p className="text-sm text-neutral-500">{file.name}</p>
          </div>
          <Button variant="ghost" size="icon">
            <MoreVertical className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Messages Area */}
      <ScrollArea className="flex-1 p-4">
        <div className="space-y-4">
          {messages.map((message) => (
            <div
              key={message.id}
              className={`flex gap-3 ${message.isCurrentUser ? 'flex-row-reverse' : ''}`}
            >
              <Avatar className="h-8 w-8 flex-shrink-0">
                <AvatarFallback className="text-xs">
                  {getInitials(message.sender)}
                </AvatarFallback>
              </Avatar>
              <div className={`flex-1 ${message.isCurrentUser ? 'items-end' : 'items-start'} flex flex-col`}>
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-sm">{message.sender}</span>
                  <span className="text-xs text-neutral-500">{message.timestamp}</span>
                </div>
                <div
                  className={`px-3 py-2 rounded-lg max-w-[80%] ${
                    message.isCurrentUser
                      ? 'bg-blue-600 text-white'
                      : 'bg-neutral-100 text-neutral-900'
                  }`}
                >
                  <p className="text-sm break-words">{message.content}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </ScrollArea>

      {/* Message Input */}
      <div className="p-4 border-t border-neutral-200">
        <div className="flex items-end gap-2">
          <div className="flex-1 space-y-2">
            <div className="flex gap-2">
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <Paperclip className="h-4 w-4" />
              </Button>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <Smile className="h-4 w-4" />
              </Button>
            </div>
            <Input
              placeholder="Nhập tin nhắn..."
              value={newMessage}
              onChange={(e) => setNewMessage(e.target.value)}
              onKeyPress={(e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault();
                  handleSendMessage();
                }
              }}
              className="resize-none"
            />
          </div>
          <Button 
            size="icon"
            onClick={handleSendMessage}
            disabled={!newMessage.trim()}
          >
            <Send className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
