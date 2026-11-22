SELECT c.Id,c.Title,c.Status,c.CreatedAt,u.Username AS Assignee,k.Name AS Customer
FROM dbo.Cases c JOIN dbo.Users u ON c.AssignedTo=u.Id JOIN dbo.Customers k ON c.CustomerId=k.Id
WHERE (@title=N'' OR c.Title LIKE N'%'+@title+N'%') AND (@status='' OR c.Status=@status)
ORDER BY c.CreatedAt DESC,c.Id DESC OFFSET @offset ROWS FETCH NEXT 21 ROWS ONLY
