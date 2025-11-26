import { FileText, Film, FileCode } from 'lucide-react';
import { ImageWithFallback } from './figma/ImageWithFallback';

interface FileViewerProps {
  file: {
    name: string;
    type: string;
    url: string;
  };
}

export function FileViewer({ file }: FileViewerProps) {
  const renderContent = () => {
    // Image files
    if (file.type.startsWith('image/')) {
      return (
        <div className="flex items-center justify-center h-full p-12">
          <ImageWithFallback
            src={file.url}
            alt={file.name}
            className="max-w-full max-h-full object-contain rounded-lg shadow-2xl"
          />
        </div>
      );
    }

    // Video files
    if (file.type.startsWith('video/')) {
      return (
        <div className="flex items-center justify-center h-full p-12">
          <video
            controls
            className="max-w-full max-h-full rounded-lg shadow-2xl"
            src={file.url}
          >
            Trình duyệt của bạn không hỗ trợ video.
          </video>
        </div>
      );
    }

    // PDF files
    if (file.type === 'application/pdf') {
      return (
        <div className="h-full w-full p-8">
          <div className="h-full bg-white rounded-lg shadow-2xl overflow-hidden">
            <iframe
              src={file.url}
              className="w-full h-full border-0"
              title={file.name}
            />
          </div>
        </div>
      );
    }

    // Text files
    if (file.type.startsWith('text/')) {
      return (
        <div className="h-full p-12">
          <div className="max-w-4xl mx-auto bg-white rounded-lg shadow-2xl p-8 h-full overflow-auto">
            <pre className="whitespace-pre-wrap break-words text-sm">
              {`// Sample content for ${file.name}
              
This is a preview of your text file. 
In a real implementation, the actual file content would be loaded here.

Example code:
function hello() {
  console.log("Hello World!");
}`}
            </pre>
          </div>
        </div>
      );
    }

    // Unsupported file type
    return (
      <div className="flex flex-col items-center justify-center h-full gap-4 text-neutral-400">
        <FileText className="h-16 w-16" />
        <p className="text-sm">Không thể xem trước loại file này</p>
        <p className="text-xs text-neutral-500">{file.type}</p>
      </div>
    );
  };

  return (
    <div className="h-full w-full bg-neutral-900">
      {renderContent()}
    </div>
  );
}