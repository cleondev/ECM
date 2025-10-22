"use client"

import type React from "react"

import { useState, useRef, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { Upload, X, FileIcon, CheckCircle2 } from "lucide-react"
import { fetchFlows, fetchTags, uploadFile } from "@/lib/api"
import type { Flow, SelectedTag, TagNode, UploadFileData, UploadMetadata } from "@/lib/types"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Input } from "@/components/ui/input"

const defaultMetadata: UploadMetadata = {
  title: "",
  docType: "General",
  status: "Draft",
  department: "",
  sensitivity: "Internal",
  description: "",
  notes: "",
}

type UploadDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onUploadComplete?: () => void
}

export function UploadDialog({ open, onOpenChange, onUploadComplete }: UploadDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [flows, setFlows] = useState<Flow[]>([])
  const [tags, setTags] = useState<TagNode[]>([])
  const [selectedFlow, setSelectedFlow] = useState<string>("")
  const [selectedTags, setSelectedTags] = useState<SelectedTag[]>([])
  const [metadata, setMetadata] = useState<UploadMetadata>(defaultMetadata)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadSuccess, setUploadSuccess] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (open) {
      fetchFlows("default").then(setFlows)
      fetchTags().then(setTags)
    }
  }, [open])

  useEffect(() => {
    if (file && !metadata.title.trim()) {
      const nameWithoutExtension = file.name.replace(/\.[^/.]+$/, "")
      setMetadata((prev) => ({ ...prev, title: nameWithoutExtension }))
    }
  }, [file, metadata.title])

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(true)
  }

  const handleDragLeave = () => {
    setIsDragging(false)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    const droppedFile = e.dataTransfer.files[0]
    if (droppedFile) {
      setFile(droppedFile)
    }
  }

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      setFile(selectedFile)
    }
  }

  const handleUpload = async () => {
    if (!file) return

    setIsUploading(true)
    try {
      const uploadData: UploadFileData = {
        file,
        flowDefinition: selectedFlow || undefined,
        metadata,
        tags: selectedTags,
      }

      await uploadFile(uploadData)
      setUploadSuccess(true)

      setTimeout(() => {
        onUploadComplete?.()
        handleClose()
      }, 1500)
    } catch (error) {
      console.error("[v0] Upload error:", error)
    } finally {
      setIsUploading(false)
    }
  }

  const handleClose = () => {
    setFile(null)
    setSelectedFlow("")
    setSelectedTags([])
    setMetadata({ ...defaultMetadata })
    setUploadSuccess(false)
    onOpenChange(false)
  }

  const toggleTag = (tag: TagNode) => {
    setSelectedTags((prev) => {
      const isSelected = prev.some((selected) => selected.id === tag.id)
      return isSelected
        ? prev.filter((selected) => selected.id !== tag.id)
        : [...prev, { id: tag.id, name: tag.name }]
    })
  }

  const getAllTags = (nodes: TagNode[]): TagNode[] => {
    const result: TagNode[] = []
    const traverse = (node: TagNode) => {
      result.push(node)
      node.children?.forEach(traverse)
    }
    nodes.forEach(traverse)
    return result
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-4xl h-[750px] flex flex-col">
        <DialogHeader>
          <DialogTitle>Upload File</DialogTitle>
        </DialogHeader>

        {uploadSuccess ? (
          <div className="flex flex-col items-center justify-center flex-1 gap-4">
            <CheckCircle2 className="h-16 w-16 text-green-500" />
            <p className="text-lg font-medium">File uploaded successfully!</p>
          </div>
        ) : (
          <div className="flex flex-col flex-1 overflow-hidden">
            <div
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
              className={`border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors mb-4 ${
                isDragging ? "border-primary bg-primary/5" : "border-border hover:border-primary/50"
              }`}
              onClick={() => fileInputRef.current?.click()}
            >
              {!file ? (
                <>
                  <Upload className="h-10 w-10 mx-auto mb-3 text-muted-foreground" />
                  <p className="font-medium mb-1">Drag and drop your file here</p>
                  <p className="text-sm text-muted-foreground mb-3">or click to browse</p>
                  <Button variant="outline" size="sm">
                    Select File
                  </Button>
                </>
              ) : (
                <div className="flex items-center justify-center gap-3">
                  <FileIcon className="h-8 w-8 text-primary" />
                  <div className="text-left">
                    <p className="font-medium">{file.name}</p>
                    <p className="text-sm text-muted-foreground">{(file.size / 1024 / 1024).toFixed(2)} MB</p>
                  </div>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={(e) => {
                      e.stopPropagation()
                      setFile(null)
                    }}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              )}
              <input ref={fileInputRef} type="file" className="hidden" onChange={handleFileSelect} />
            </div>

            <Tabs defaultValue="tags" className="flex-1 flex flex-col overflow-hidden">
              <TabsList className="grid w-full grid-cols-3">
                <TabsTrigger value="tags">Tags</TabsTrigger>
                <TabsTrigger value="flow">Flow</TabsTrigger>
                <TabsTrigger value="metadata">Metadata</TabsTrigger>
              </TabsList>

              <TabsContent value="tags" className="flex-1 overflow-y-auto mt-4">
                <div className="space-y-2">
                  <Label>Select Tags</Label>
                  <div className="flex flex-wrap gap-2 min-h-[200px] p-3 border rounded-md">
                    {getAllTags(tags).map((tag) => (
                      <Badge
                        key={tag.id}
                        variant={selectedTags.some((selected) => selected.id === tag.id) ? "default" : "outline"}
                        className="cursor-pointer h-fit"
                        onClick={() => toggleTag(tag)}
                        style={
                          selectedTags.some((selected) => selected.id === tag.id) && tag.color
                            ? { backgroundColor: tag.color, borderColor: tag.color }
                            : {}
                        }
                      >
                        {tag.icon && <span className="mr-1">{tag.icon}</span>}
                        {tag.name}
                      </Badge>
                    ))}
                  </div>
                  {selectedTags.length > 0 && (
                    <p className="text-sm text-muted-foreground">
                      {selectedTags.length} tag{selectedTags.length > 1 ? "s" : ""} selected
                    </p>
                  )}
                </div>
              </TabsContent>

              <TabsContent value="flow" className="flex-1 overflow-y-auto mt-4">
                <div className="space-y-2">
                  <Label>Select Flow (Optional)</Label>
                  <Select value={selectedFlow} onValueChange={setSelectedFlow}>
                    <SelectTrigger>
                      <SelectValue placeholder="Choose a workflow" />
                    </SelectTrigger>
                    <SelectContent>
                      {flows.map((flow) => (
                        <SelectItem key={flow.id} value={flow.id}>
                          {flow.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {selectedFlow && (
                    <p className="text-sm text-muted-foreground">
                      Selected flow will be applied to the file after upload
                    </p>
                  )}
                </div>
              </TabsContent>

              <TabsContent value="metadata" className="flex-1 overflow-y-auto mt-4">
                <div className="space-y-6">
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="title">Title</Label>
                      <Input
                        id="title"
                        placeholder="Enter document title"
                        value={metadata.title}
                        onChange={(e) => setMetadata((prev) => ({ ...prev, title: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="department">Department</Label>
                      <Input
                        id="department"
                        placeholder="Enter department"
                        value={metadata.department}
                        onChange={(e) => setMetadata((prev) => ({ ...prev, department: e.target.value }))}
                      />
                    </div>
                  </div>

                  <div className="grid gap-4 md:grid-cols-3">
                    <div className="space-y-2">
                      <Label>Document type</Label>
                      <Select
                        value={metadata.docType}
                        onValueChange={(value) => setMetadata((prev) => ({ ...prev, docType: value }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select document type" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="General">General</SelectItem>
                          <SelectItem value="Contract">Contract</SelectItem>
                          <SelectItem value="Policy">Policy</SelectItem>
                          <SelectItem value="Report">Report</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-2">
                      <Label>Status</Label>
                      <Select
                        value={metadata.status}
                        onValueChange={(value) => setMetadata((prev) => ({ ...prev, status: value }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select status" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Draft">Draft</SelectItem>
                          <SelectItem value="InReview">In review</SelectItem>
                          <SelectItem value="Published">Published</SelectItem>
                          <SelectItem value="Archived">Archived</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-2">
                      <Label>Sensitivity</Label>
                      <Select
                        value={metadata.sensitivity}
                        onValueChange={(value) => setMetadata((prev) => ({ ...prev, sensitivity: value }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select sensitivity" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Public">Public</SelectItem>
                          <SelectItem value="Internal">Internal</SelectItem>
                          <SelectItem value="Confidential">Confidential</SelectItem>
                          <SelectItem value="Restricted">Restricted</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="description">Description</Label>
                    <Textarea
                      id="description"
                      placeholder="Enter file description..."
                      value={metadata.description}
                      onChange={(e) => setMetadata((prev) => ({ ...prev, description: e.target.value }))}
                      rows={3}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="notes">Notes</Label>
                    <Textarea
                      id="notes"
                      placeholder="Additional notes..."
                      value={metadata.notes}
                      onChange={(e) => setMetadata((prev) => ({ ...prev, notes: e.target.value }))}
                      rows={3}
                    />
                  </div>
                </div>
              </TabsContent>
            </Tabs>

            <div className="flex justify-end gap-2 pt-4 border-t mt-4">
              <Button variant="outline" onClick={handleClose}>
                Cancel
              </Button>
              <Button onClick={handleUpload} disabled={!file || isUploading}>
                {isUploading ? "Uploading..." : "Upload File"}
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
