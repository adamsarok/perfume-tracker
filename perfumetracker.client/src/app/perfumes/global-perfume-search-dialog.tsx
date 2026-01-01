"use client";

import { useState, useEffect } from "react";
import { GlobalPerfumeDTO } from "@/dto/GlobalPerfumeDTO";
import { searchGlobalPerfumes, addPerfumeFromGlobal } from "@/services/global-perfume-service";
import { showError, showSuccess } from "@/services/toasty-service";
import { Search, Plus } from "lucide-react";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";


interface GlobalPerfumeSearchDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onPerfumeAdded: (perfumeId: string) => void;
}

export default function GlobalPerfumeSearchDialog({
  open,
  onOpenChange,
  onPerfumeAdded,
}: GlobalPerfumeSearchDialogProps) {
  const [searchText, setSearchText] = useState("");
  const [results, setResults] = useState<GlobalPerfumeDTO[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [selectedPerfume, setSelectedPerfume] = useState<GlobalPerfumeDTO | null>(null);
  const [ml, setMl] = useState<number>(50);
  const [mlLeft, setMlLeft] = useState<number>(50);
  const [isAdding, setIsAdding] = useState(false);

  useEffect(() => {
    const timeoutId = setTimeout(async () => {
      if (searchText.length >= 2) {
        setIsSearching(true);
        const result = await searchGlobalPerfumes(searchText);
        setIsSearching(false);
        
        if (result.error) {
          showError("Search failed", result.error);
          setResults([]);
        } else {
          setResults(result.data || []);
        }
      } else {
        setResults([]);
      }
    }, 300);

    return () => clearTimeout(timeoutId);
  }, [searchText]);

  const handleSelectPerfume = (perfume: GlobalPerfumeDTO) => {
    setSelectedPerfume(perfume);
  };

  const handleAddPerfume = async () => {
    if (!selectedPerfume) return;

    setIsAdding(true);
    const result = await addPerfumeFromGlobal(selectedPerfume.id, ml, mlLeft);
    setIsAdding(false);

    if (result.error || !result.data) {
      showError("Failed to add perfume", result.error ?? "unknown error");
    } else {
      showSuccess(`Added ${selectedPerfume.house} - ${selectedPerfume.perfumeName}`);
      onPerfumeAdded(result.data.id);
      onOpenChange(false);
      setSearchText("");
      setResults([]);
      setSelectedPerfume(null);
    }
  };

  const handleCancel = () => {
    setSelectedPerfume(null);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[80vh]">
        <DialogHeader>
          <DialogTitle>Search Global Perfume Database</DialogTitle>
          <DialogDescription>
            Search from 27,000+ perfumes. Enter house name or perfume name.
          </DialogDescription>
        </DialogHeader>

        {!selectedPerfume ? (
          <div className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search by house or perfume name..."
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                className="pl-10"
                autoFocus
              />
            </div>

            {isSearching && <div className="text-center text-sm text-muted-foreground">Searching...</div>}

            {!isSearching && results.length === 0 && searchText.length >= 2 && (
              <div className="text-center text-sm text-muted-foreground">No results found</div>
            )}

            {results.length > 0 && (
              <ScrollArea className="h-[400px] rounded-md border p-4">
                <div className="space-y-2">
                  {results.map((perfume) => (
                    <div
                      key={perfume.id}
                      className="p-3 border rounded-lg hover:bg-accent cursor-pointer transition-colors"
                      onClick={() => handleSelectPerfume(perfume)}
                    >
                      <div className="font-semibold">{perfume.house}</div>
                      <div className="text-sm text-muted-foreground">{perfume.perfumeName}</div>
                      <div className="text-xs text-muted-foreground mt-1">{perfume.family}</div>
                      {perfume.tags.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-2">
                          {perfume.tags.slice(0, 5).map((tag) => (
                            <span
                              key={tag.id}
                              className="px-2 py-1 text-xs rounded-md"
                              style={{ backgroundColor: tag.color, color: "#fff" }}
                            >
                              {tag.tagName}
                            </span>
                          ))}
                          {perfume.tags.length > 5 && (
                            <span className="px-2 py-1 text-xs text-muted-foreground">
                              +{perfume.tags.length - 5} more
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </ScrollArea>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <div className="p-4 border rounded-lg bg-accent">
              <div className="font-semibold text-lg">{selectedPerfume.house}</div>
              <div className="text-muted-foreground">{selectedPerfume.perfumeName}</div>
              <div className="text-sm text-muted-foreground mt-1">Family: {selectedPerfume.family}</div>
              {selectedPerfume.tags.length > 0 && (
                <div className="flex flex-wrap gap-1 mt-2">
                  {selectedPerfume.tags.map((tag) => (
                    <span
                      key={tag.id}
                      className="px-2 py-1 text-xs rounded-md"
                      style={{ backgroundColor: tag.color, color: "#fff" }}
                    >
                      {tag.tagName}
                    </span>
                  ))}
                </div>
              )}
            </div>

            <div className="space-y-3">
              <div>
                <Label htmlFor="ml">Bottle Size (ml)</Label>
                <Input
                  id="ml"
                  type="number"
                  min="0"
                  max="1000"
                  value={ml}
                  onChange={(e) => setMl(Number(e.target.value))}
                />
              </div>
              <div>
                <Label htmlFor="mlLeft">Amount Remaining (ml)</Label>
                <Input
                  id="mlLeft"
                  type="number"
                  min="0"
                  max="1000"
                  value={mlLeft}
                  onChange={(e) => setMlLeft(Number(e.target.value))}
                />
              </div>
            </div>

            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={handleCancel} disabled={isAdding}>
                Back
              </Button>
              <Button onClick={handleAddPerfume} disabled={isAdding}>
                <Plus className="mr-2" />
                {isAdding ? "Adding..." : "Add to Collection"}
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}