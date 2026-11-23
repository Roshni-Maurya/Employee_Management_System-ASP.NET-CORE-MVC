SELECT u.Email, r.Name AS Role
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON r.Id = ur.RoleId
WHERE r.Name = 'Admin'

DELETE FROM AspNetUsers
WHERE Id IN (
    SELECT ur.UserId FROM AspNetUserRoles ur
    JOIN AspNetRoles r ON r.Id = ur.RoleId
    WHERE r.Name = 'Admin')