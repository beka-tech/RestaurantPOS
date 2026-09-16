import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TableList } from './table-list';

describe('TableList', () => {
  let component: TableList;
  let fixture: ComponentFixture<TableList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TableList],
    }).compileComponents();

    fixture = TestBed.createComponent(TableList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
