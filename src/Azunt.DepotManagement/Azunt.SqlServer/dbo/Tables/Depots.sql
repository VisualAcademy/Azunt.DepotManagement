--[0][0] 창고: Depots 
CREATE TABLE [dbo].[Depots]
(
    [Id]        BIGINT             IDENTITY (1, 1) NOT NULL PRIMARY KEY,    -- 창고 고유 아이디, 자동 증가
    [Active]    BIT                DEFAULT ((1)) NOT NULL,                  -- 활성 상태 표시, 기본값 1 (활성)
    [CreatedAt] DATETIMEOFFSET (7) NOT NULL,                                -- 레코드 생성 시간
    [CreatedBy] NVARCHAR (255)     NULL,                                    -- 레코드 생성자 이름
    [Name]      NVARCHAR (MAX)     NULL,                                    -- 이름
    [IsDeleted] BIT NOT NULL DEFAULT(0)                                     -- Soft Delete 플래그
);
