create table DataTable (
   Id INT not null,
   [Text] Text null,
   primary key (Id)
);

insert into DataTable (Id, [Text]) VALUES (1, 'some text');
