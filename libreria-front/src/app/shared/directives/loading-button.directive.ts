import { Directive, HostListener, HostBinding, Input } from '@angular/core';

@Directive({
    selector: '[appLoadingButton]',
    standalone: true
})
export class LoadingButtonDirective {
    @Input() appLoadingButton = false;

    @HostBinding('disabled')
    get isDisabled(): boolean {
        return this.appLoadingButton;
    }

    @HostBinding('class.loading')
    get isLoading(): boolean {
        return this.appLoadingButton;
    }

    @HostListener('click', ['$event'])
    onClick(event: MouseEvent): void {
        if (this.appLoadingButton) {
            event.preventDefault();
            event.stopPropagation();
        }
    }
}
