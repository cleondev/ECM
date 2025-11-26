import { useState } from 'react';
import { Send, Smile, Paperclip, AtSign } from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { ScrollArea } from '../ui/scroll-area';
import { Badge } from '../ui/badge';

interface ChatTabProps {
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
  type?: 'message' | 'system';
}

const mockMessages: Message[] = [
  {
    id: '0',
    sender: 'System',
    content: 'Tran Thi B đã tham gia xem tài liệu',
    timestamp: '10:25',
    isCurrentUser: false,
    type: 'system'
  },
  {
    id: '1',
    sender: 'Tran Thi B',
    content: 'Chào mọi người! Mình vừa mở file này.',
    timestamp: '10:30',
    isCurrentUser: false
  },
  {
    id: '2',
    sender: 'Bạn',
    content: 'Chào B! Mình đang xem phần proposal ở trang 3.',
    timestamp: '10:32',
    isCurrentUser: true
  },
  {
    id: '3',
    sender: 'Le Van C',
    content: 'Mọi người có thể xem qua phần budget không? Mình thấy có vấn đề.',
    timestamp: '10:35',
    isCurrentUser: false
  },
  {
    id: '4',
    sender: 'Bạn',
    content: 'Ok, để mình check lại phần đó.',
    timestamp: '10:36',
    isCurrentUser: true
  },
  {
    id: '5',
    sender: 'System',
    content: 'Le Van C đang gõ...',
    timestamp: '10:37',
    isCurrentUser: false,
    type: 'system'
  }
];

export function ChatTab({ file }: ChatTabProps) {
  const [messages, setMessages] = useState<Message[]>(mockMessages);
  const [newMessage, setNewMessage] = useState('');
  const [isTyping, setIsTyping] = useState(false);

  const handleSendMessage = () => {
    if (newMessage.trim()) {
      const message: Message = {
        id: Date.now().toString(),
        sender: 'Bạn',
        content: newMessage,
        timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
        isCurrentUser: true,
        type: 'message'
      };
      setMessages([...messages.filter(m => m.type !== 'system' || !m.content.includes('đang gõ')), message]);
      setNewMessage('');
      setIsTyping(false);
    }
  };

  const handleTyping = (value: string) => {
    setNewMessage(value);
    if (value.length > 0 && !isTyping) {
      setIsTyping(true);
    } else if (value.length === 0 && isTyping) {
      setIsTyping(false);
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
      <div className="p-3 border-b border-neutral-200 bg-blue-50">
        <div className="flex items-center gap-2">
          <div className="h-2 w-2 rounded-full bg-green-500 animate-pulse" />
          <p className="text-xs">
            <span className="font-medium">3 người</span> đang xem tài liệu
          </p>
        </div>
      </div>

      {/* Messages Area */}
      <ScrollArea className="flex-1 p-4">
        <div className="space-y-4">
          {messages.map((message) => {
            if (message.type === 'system') {
              return (
                <div key={message.id} className="flex justify-center">
                  <Badge variant="secondary" className="text-xs">
                    {message.content}
                  </Badge>
                </div>
              );
            }

            return (
              <div
                key={message.id}
                className={`flex gap-2 ${message.isCurrentUser ? 'flex-row-reverse' : ''}`}
              >
                <Avatar className="h-7 w-7 flex-shrink-0">
                  <AvatarFallback className="text-xs">
                    {getInitials(message.sender)}
                  </AvatarFallback>
                </Avatar>
                <div className={`flex-1 ${message.isCurrentUser ? 'items-end' : 'items-start'} flex flex-col`}>
                  <div className="flex items-center gap-2 mb-1">
                    <span className="text-xs">{message.sender}</span>
                    <span className="text-xs text-neutral-500">{message.timestamp}</span>
                  </div>
                  <div
                    className={`px-3 py-1.5 rounded-lg max-w-[85%] ${
                      message.isCurrentUser
                        ? 'bg-blue-600 text-white'
                        : 'bg-neutral-100 text-neutral-900'
                    }`}
                  >
                    <p className="text-xs break-words">{message.content}</p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </ScrollArea>

      {/* Message Input */}
      <div className="p-3 border-t border-neutral-200">
        <div className="flex items-center gap-2 mb-2">
          <Button variant="ghost" size="icon" className="h-7 w-7">
            <Paperclip className="h-3.5 w-3.5" />
          </Button>
          <Button variant="ghost" size="icon" className="h-7 w-7">
            <AtSign className="h-3.5 w-3.5" />
          </Button>
          <Button variant="ghost" size="icon" className="h-7 w-7">
            <Smile className="h-3.5 w-3.5" />
          </Button>
        </div>
        <div className="flex gap-2">
          <Input
            placeholder="Nhập tin nhắn về tài liệu..."
            value={newMessage}
            onChange={(e) => handleTyping(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                handleSendMessage();
              }
            }}
            className="h-8 text-sm"
          />
          <Button 
            size="icon"
            onClick={handleSendMessage}
            disabled={!newMessage.trim()}
            className="h-8 w-8"
          >
            <Send className="h-3.5 w-3.5" />
          </Button>
        </div>
      </div>
    </div>
  );
}