using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Azunt.DepotManagement
{
    /// <summary>
    /// Depots 테이블과 매핑되는 창고(Depot) 엔터티 클래스입니다.
    /// </summary>
    [Table("Depots")]
    public class Depot
    {
        /// <summary>
        /// 창고 고유 아이디 (자동 증가)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// 활성 상태 (기본값: true)
        /// </summary>
        public bool? Active { get; set; }

        /// <summary>
        /// 생성 일시
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// 생성자 이름
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 창고 이름
        /// </summary>
        public string? Name { get; set; }
    }
}