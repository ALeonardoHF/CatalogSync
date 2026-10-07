import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";

export interface Notification {
    message: string;
    type: 'success' | 'error' | 'info';
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
    private notification$ = new BehaviorSubject<Notification | null>(null);

    current$ = this.notification$.asObservable();

    show(message: string, type: Notification['type'] = 'info'): void {
        this.notification$.next({ message, type });
        setTimeout(() => this.notification$.next(null), 3000);
    }

    clear(): void {
        this.notification$.next(null);
    }
}
