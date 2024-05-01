import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/services/environment';
import { Website } from 'src/models/website-list.model';
import { map } from 'rxjs/operators';
@Injectable({
  providedIn: 'root',
})
export class WebsiteService {
  constructor(private http: HttpClient) {}

  uploadWebsite(model: FormData): Observable<any> {
    return this.http.post<any>(
      `${environment.apiUrl}/api/uploadWebsite`,
      model
    );
  }

  getAllWebsites(): Observable<Website[]> {
    return this.http.get<Website[]>(
      `${environment.apiUrl}/api/getWebsitesList`
    );
  }

  getWebsite(id: string){
    return this.getAllWebsites().pipe(
      map(websites => websites.find(website => website.WebsiteId === id))
    );
  }

  deleteWebsite(WebsiteId: string) {
    return this.http.delete(
      `${environment.apiUrl}/api/delete/website/${WebsiteId}`
    );
  }

  updateWesbite(website: Website){

  }
}
