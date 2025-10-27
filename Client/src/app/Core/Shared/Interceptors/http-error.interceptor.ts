import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { HttpStatus } from '../../Shared/Enum/httpStatus.enum';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  constructor(private toastr: ToastrService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        let errorMessage = '';
         if (error.status === 0) {
          errorMessage = 'Server is not reachable. Please check your connection or try again later.';
          this.toastr.error(errorMessage);
          return throwError(() => error);
        }
        if (error.error?.message) {
          errorMessage = error.error.message;

        try {
            const jsonPart = errorMessage.substring(errorMessage.indexOf('{'));
            const parsed = JSON.parse(jsonPart);
            if (parsed.loginId) {
              errorMessage = 'Invalid Aadhaar number';
            }
          } catch {
          }
        }

        if (error.status === HttpStatus.BadRequest) this.toastr.error(errorMessage);
        else if (error.status === HttpStatus.NotFound) this.toastr.warning(errorMessage);
        else if (error.status === HttpStatus.TooManyRequests) this.toastr.warning(errorMessage);
        else this.toastr.error(errorMessage);

        return throwError(() => error);
      })
    );
  }
}

