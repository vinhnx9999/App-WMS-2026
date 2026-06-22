import { useRef } from "react";
import { useTranslation } from "react-i18next";
import { Loader2, Upload, Download } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

interface SkuImportUploadProps {
  uploading: boolean;
  onUpload: (file: File) => void;
  onDownloadTemplate: () => void;
}

export function SkuImportUpload({
  uploading,
  onUpload,
  onDownloadTemplate
}: SkuImportUploadProps) {
  const { t } = useTranslation();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (uploading) return;
    const files = e.dataTransfer.files;
    if (files && files.length > 0) {
      onUpload(files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files && files.length > 0) {
      onUpload(files[0]);
    }
  };

  const handleClick = () => {
    if (!uploading) {
      fileInputRef.current?.click();
    }
  };

  return (
    <Card className="flex-1 w-full bg-card border border-border flex flex-col">
      <CardHeader className="flex flex-row items-center gap-4 pb-4 border-b border-border shrink-0">
        <div>
          <CardTitle className="text-xl font-bold tracking-tight">
            {t("translation:skus.import.newImport")}
          </CardTitle>
          <CardDescription className="text-sm text-muted-foreground">
            Tải lên file Excel để kiểm tra và import SKU vào kho hàng.
          </CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex-1 flex flex-col items-center justify-center p-8">
        <div
          onDragOver={(e) => e.preventDefault()}
          onDrop={handleFileDrop}
          onClick={handleClick}
          className="w-full max-w-xl border-2 border-dashed border-muted hover:border-primary/50 transition-all rounded-xl p-12 flex flex-col items-center justify-center gap-4 bg-muted/20 hover:bg-muted/40 cursor-pointer text-center group"
        >
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            accept=".xlsx, .xls"
            className="hidden"
            disabled={uploading}
          />
          {uploading ? (
            <div className="flex flex-col items-center gap-3">
              <Loader2 className="size-10 animate-spin text-primary" />
              <span className="text-sm font-semibold">{t("translation:skus.import.uploading")}</span>
            </div>
          ) : (
            <>
              <div className="p-4 bg-card rounded-full border border-border shadow-sm group-hover:scale-110 transition-transform duration-200">
                <Upload className="size-8 text-primary" />
              </div>
              <div>
                <p className="text-base font-semibold text-foreground group-hover:text-primary transition-colors">
                  {t("translation:skus.import.dragDropArea")}
                </p>
                <p className="text-xs text-muted-foreground mt-1.5">
                  {t("translation:skus.import.dragDropSub")}
                </p>
              </div>
            </>
          )}
        </div>
        <div className="mt-8 flex gap-4">
          <Button variant="outline" onClick={onDownloadTemplate} className="cursor-pointer text-sm gap-1.5">
            <Download className="size-4" />
            {t("translation:skus.import.downloadTemplate")}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
