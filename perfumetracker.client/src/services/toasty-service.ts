import { toast } from "react-toastify";

export function showError(msg: string, error: unknown = '') {
    toast.error(msg + error);
    console.error(msg, error);
}

export function showSuccess(msg: string) {
    toast.success(msg);
}