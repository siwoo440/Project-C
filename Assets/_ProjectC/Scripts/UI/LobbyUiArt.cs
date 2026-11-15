using System.Collections.Generic; // 공용 이미지 검색표 사용
using UnityEngine; // 로비 이미지 리소스 사용
using UnityEngine.UI; // 화면 이미지 사용

public static class LobbyUiArt // 공통 로비 이미지 제공
{ // 로비 이미지 제공 시작
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(); // 생성 이미지 보관
    public static void Apply(Image target, string name) // 지정 로비 이미지 적용
    { // 로비 이미지 적용 시작
        if (target == null || string.IsNullOrWhiteSpace(name)) // 화면과 이미지 이름 확인
        { // 이미지 연결 불가 처리 시작
            return; // 기존 배경 유지
        } // 이미지 연결 불가 처리 종료
        if (!sprites.TryGetValue(name, out Sprite sprite) || sprite == null) // 생성된 이미지 보관 여부 확인
        { // 로비 이미지 준비 시작
            string path = "UI/Lobby/" + name; // 로비 이미지 리소스 경로 생성
            Texture2D texture = Resources.Load<Texture2D>(path); // 제작 이미지 텍스처 조회
            if (texture != null) // 이미지 텍스처 존재 확인
            { // 텍스처 이미지 준비 시작
                Rect area = GetImageArea(name, texture); // 불필요한 투명 여백 제외 영역 조회
                sprite = Sprite.Create(texture, area, new Vector2(0.5f, 0.5f)); // 이미지 표시 영역 스프라이트 생성
                sprite.hideFlags = HideFlags.HideAndDontSave; // 런타임 공용 이미지 보관 설정
            } // 텍스처 이미지 준비 종료
            else // 텍스처 리소스 없음 확인
            { // 단일 이미지 확인 시작
                sprite = Resources.Load<Sprite>(path); // 단일 스프라이트 리소스 조회
            } // 단일 이미지 확인 종료
            if (sprite == null) // 이미지 리소스 존재 확인
            { // 이미지 누락 처리 시작
                Debug.LogWarning("[LobbyUI] 이미지를 찾을 수 없습니다: " + path); // 누락 이미지 안내
                return; // 기존 배경 유지
            } // 이미지 누락 처리 종료
            sprites[name] = sprite; // 재사용할 이미지 보관
        } // 로비 이미지 준비 종료
        target.sprite = sprite; // 제작한 이미지 연결
        target.color = Color.white; // 이미지 원본 색상 유지
    } // 로비 이미지 적용 종료
    private static Rect GetImageArea(string name, Texture2D texture) // 이미지의 실제 표시 영역 계산
    { // 이미지 영역 계산 시작
        if (name == "Button") // 로비 버튼 이미지 확인
        { // 버튼 여백 조정 시작
            return new Rect(texture.width * 0.02f, texture.height * 0.23f, texture.width * 0.96f, texture.height * 0.54f); // 버튼 외부 투명 여백 제외
        } // 버튼 여백 조정 종료
        if (name == "Panel") // 자원 패널 이미지 확인
        { // 자원 패널 여백 조정 시작
            return new Rect(texture.width * 0.01f, texture.height * 0.30f, texture.width * 0.98f, texture.height * 0.40f); // 자원 패널 외부 투명 여백 제외
        } // 자원 패널 여백 조정 종료
        if (name == "Settings" || name == "Help") // 설정과 도움말 이미지 확인
        { // 정사각형 버튼 여백 조정 시작
            return new Rect(texture.width * 0.09f, texture.height * 0.09f, texture.width * 0.82f, texture.height * 0.82f); // 버튼 외부 투명 여백 제외
        } // 정사각형 버튼 여백 조정 종료
        if (name == "FormationPanel") // 편성 화면 패널 이미지 확인
        { // 편성 패널 여백 조정 시작
            return new Rect(texture.width * 0.09f, texture.height * 0.02f, texture.width * 0.82f, texture.height * 0.96f); // 패널 외부 투명 여백 제외
        } // 편성 패널 여백 조정 종료
        if (name == "FormationCard") // 캐릭터 편성 칸 이미지 확인
        { // 편성 칸 여백 조정 시작
            return new Rect(texture.width * 0.08f, texture.height * 0.035f, texture.width * 0.84f, texture.height * 0.93f); // 편성 칸 외부 투명 여백 제외
        } // 편성 칸 여백 조정 종료
        if (name == "Gold") // 골드 아이콘 확인
        { // 골드 여백 조정 시작
            return new Rect(texture.width * 0.24f, texture.height * 0.18f, texture.width * 0.52f, texture.height * 0.64f); // 골드 외부 투명 여백 제외
        } // 골드 여백 조정 종료
        if (name == "Screw") // 나사 아이콘 확인
        { // 나사 여백 조정 시작
            return new Rect(texture.width * 0.26f, texture.height * 0.06f, texture.width * 0.48f, texture.height * 0.88f); // 나사 외부 투명 여백 제외
        } // 나사 여백 조정 종료
        if (name == "Iron") // 철판 아이콘 확인
        { // 철판 여백 조정 시작
            return new Rect(texture.width * 0.23f, texture.height * 0.17f, texture.width * 0.54f, texture.height * 0.68f); // 철판 외부 투명 여백 제외
        } // 철판 여백 조정 종료
        if (name == "Wire") // 전선 아이콘 확인
        { // 전선 여백 조정 시작
            return new Rect(texture.width * 0.24f, texture.height * 0.10f, texture.width * 0.52f, texture.height * 0.80f); // 전선 외부 투명 여백 제외
        } // 전선 여백 조정 종료
        return new Rect(0f, 0f, texture.width, texture.height); // 자원 아이콘 원본 영역 반환
    } // 이미지 영역 계산 종료
} // 로비 이미지 제공 종료
