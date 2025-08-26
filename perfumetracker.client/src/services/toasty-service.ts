import { toast } from "sonner";

export function showError(msg?: string, error?: unknown) {
  let err = '';
  if (error instanceof Error) {
    err = error.message;
  } else if (typeof error === 'string') {
    err = error;
  } else if (error) {
    err = JSON.stringify(error);
  }
  const composed = [msg, err].filter(Boolean).join(': ');
  toast.error(composed);
  console.error(msg ?? 'Error', error);
}

export function showSuccess(msg: string) {
    toast.success(msg);
}