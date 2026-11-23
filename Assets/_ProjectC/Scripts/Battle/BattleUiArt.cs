using System.Collections.Generic; // 생성된 전투 이미지 재사용 목록
using UnityEngine; // 전투 이미지와 스프라이트 사용
using UnityEngine.UI; // 전투 화면 이미지 사용
public static class BattleUiArt // 이미지 생성으로 제작한 전투 UI 제공
{ // 전투 이미지 제공 시작
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(); // 재사용할 전투 스프라이트 목록
    public static bool Apply(Image target, string name) // 지정한 전투 이미지 적용
    { // 전투 이미지 적용 시작
        if (target == null || name != "HandPanel" && name != "CardFrame" && name != "CostBadge" && name != "GaugeFrame" && name != "VitalGaugeFrame" && name != "HealthGaugeIcon" && name != "MentalGaugeIcon") // 화면과 지원 이미지 이름 확인
        { // 이미지 적용 불가 처리 시작
            return false; // 기존 화면 유지
        } // 이미지 적용 불가 처리 종료
        if (!sprites.TryGetValue(name, out Sprite sprite) || sprite == null) // 기존 전투 이미지 확인
        { // 전투 이미지 준비 시작
            Texture2D texture = Resources.Load<Texture2D>("UI/Battle/" + name); // 생성 이미지 텍스처 조회
            if (texture == null) // 생성 이미지 누락 확인
            { // 이미지 누락 처리 시작
                return false; // 기존 배경과 색상 유지
            } // 이미지 누락 처리 종료
            bool hand = name == "HandPanel"; // 손패 배경 이미지 확인
            Rect area = GetArtArea(texture, name); // 이미지별 투명 여백 제외 영역 조회
            Vector4 border = hand ? new Vector4(area.width * 0.025f, area.height * 0.06f, area.width * 0.025f, area.height * 0.06f) : new Vector4(area.width * 0.04f, area.height * 0.04f, area.width * 0.04f, area.height * 0.04f); // 크기 변경 시 유지할 테두리 범위
            sprite = Sprite.Create(texture, area, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border); // 테두리 보존 전투 스프라이트 생성
            sprite.hideFlags = HideFlags.HideAndDontSave; // 공용 런타임 이미지 저장 제외
            sprites[name] = sprite; // 생성 이미지 재사용 목록 보관
        } // 전투 이미지 준비 종료
        target.sprite = sprite; // 전투 화면에 생성 이미지 연결
        target.type = name == "HandPanel" ? Image.Type.Sliced : Image.Type.Simple; // 손패 모서리 보존과 카드 원본 비율 적용
        target.color = Color.white; // 이미지 원본 색상 적용
        return true; // 이미지 적용 성공 반환
    } // 전투 이미지 적용 종료
    private static Rect GetArtArea(Texture2D texture, string name) // 제작 이미지의 실제 그림 영역 조회
    { // 그림 영역 조회 시작
        if (name == "HandPanel") // 손패 배경 이미지 확인
        { // 손패 배경 영역 반환 시작
            return new Rect(texture.width * 0.013f, texture.height * 0.134f, texture.width * 0.974f, texture.height * 0.734f); // 손패 배경 그림 영역 반환
        } // 손패 배경 영역 반환 종료
        if (name == "CardFrame") // 카드 프레임 이미지 확인
        { // 카드 프레임 영역 반환 시작
            return new Rect(texture.width * 0.066f, texture.height * 0.044f, texture.width * 0.868f, texture.height * 0.897f); // 카드 프레임 그림 영역 반환
        } // 카드 프레임 영역 반환 종료
        if (name == "CostBadge") // 카드 비용 이미지 확인
        { // 카드 비용 영역 반환 시작
            return new Rect(texture.width * 0.249f, texture.height * 0.253f, texture.width * 0.498f, texture.height * 0.495f); // 카드 비용 그림 영역 반환
        } // 카드 비용 영역 반환 종료
        if (name == "VitalGaugeFrame") // 체력과 정신력 공통 프레임 확인
        { // 공통 프레임 영역 반환 시작
            return new Rect(texture.width * 0.032f, texture.height * 0.38f, texture.width * 0.94f, texture.height * 0.25f); // 투명 바 프레임 그림 영역 반환
        } // 공통 프레임 영역 반환 종료
        if (name == "HealthGaugeIcon") // 체력 아이콘 이미지 확인
        { // 체력 아이콘 영역 반환 시작
            return new Rect(texture.width * 0.19f, texture.height * 0.19f, texture.width * 0.62f, texture.height * 0.62f); // 하트 아이콘 그림 영역 반환
        } // 체력 아이콘 영역 반환 종료
        if (name == "MentalGaugeIcon") // 정신력 아이콘 이미지 확인
        { // 정신력 아이콘 영역 반환 시작
            return new Rect(texture.width * 0.15f, texture.height * 0.16f, texture.width * 0.7f, texture.height * 0.7f); // 정신력 아이콘 그림 영역 반환
        } // 정신력 아이콘 영역 반환 종료
        return new Rect(texture.width * 0.027f, texture.height * 0.386f, texture.width * 0.946f, texture.height * 0.234f); // 기존 게이지 그림 영역 반환
    } // 그림 영역 조회 종료
} // 전투 이미지 제공 종료
