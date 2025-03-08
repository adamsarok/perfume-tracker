import { toast } from "sonner";

export function showError(msg: string | undefined, error: unknown = '') {
    toast.error(msg ?? '' + error);
    console.error(msg, error);
}

export function showSuccess(msg: string) {
    toast.success(msg);
}