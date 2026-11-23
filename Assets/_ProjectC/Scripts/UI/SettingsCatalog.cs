public sealed class SettingItem // 기획서의 설정 항목 정보
{ // 설정 항목 시작
    public readonly string Name; // 설정 항목 이름
    public readonly string DefaultValue; // 기획서 기본값
    public SettingItem(string name, string defaultValue) // 설정 항목 생성
    { // 설정 항목 생성 시작
        Name = name; // 설정 이름 보관
        DefaultValue = defaultValue; // 기획 기본값 보관
    } // 설정 항목 생성 종료
} // 설정 항목 종료
public static class SettingsCatalog // 기획서의 설정 분류와 기본값
{ // 설정 목록 시작
    public static readonly string[] Tabs = { "그래픽", "사운드", "게임 플레이", "조작", "언어" }; // 설정 다섯 분류
    public static readonly SettingItem[][] Items = // 설정 분류별 항목 목록
    { // 전체 설정 항목 시작
        new SettingItem[] // 그래픽 설정 항목
        { // 그래픽 항목 시작
            new SettingItem("화면 모드", "무테창"), // 화면 모드 기본값
            new SettingItem("해상도", "1920×1080"), // 해상도 기본값
            new SettingItem("프레임 제한", "60"), // 프레임 제한 기본값
            new SettingItem("V-Sync", "끄기"), // 수직 동기화 기본값
            new SettingItem("밝기(감마)", "1.00"), // 화면 밝기 기본값
            new SettingItem("UI 스케일", "100%"), // 화면 크기 기본값
            new SettingItem("텍스트 크기", "보통"), // 글자 크기 기본값
            new SettingItem("그래픽 품질 프리셋", "높음"), // 그래픽 품질 기본값
            new SettingItem("그림자 품질", "중간"), // 그림자 품질 기본값
            new SettingItem("후처리", "켜기"), // 후처리 기본값
            new SettingItem("화면 흔들림", "보통"), // 화면 흔들림 기본값
            new SettingItem("파티클/특수효과 밀도", "중간"), // 특수효과 밀도 기본값
            new SettingItem("색약 보정", "끄기") // 색각 보정 기본값
        }, // 그래픽 항목 종료
        new SettingItem[] // 사운드 설정 항목
        { // 사운드 항목 시작
            new SettingItem("마스터 볼륨", "80"), // 전체 음량 기본값
            new SettingItem("BGM 볼륨", "70"), // 배경 음악 기본값
            new SettingItem("효과음(SFX) 볼륨", "80"), // 효과음 기본값
            new SettingItem("UI 사운드 볼륨", "60"), // 화면 효과음 기본값
            new SettingItem("보이스 볼륨", "75"), // 음성 기본값
            new SettingItem("환경음/앰비언트 볼륨", "65"), // 환경음 기본값
            new SettingItem("동적 음량", "보통"), // 자동 음량 기본값
            new SettingItem("출력 장치 선택", "기본 장치"), // 출력 장치 기본값
            new SettingItem("자막 표시", "켜기") // 자막 표시 기본값
        }, // 사운드 항목 종료
        new SettingItem[] // 게임 플레이 설정 항목
        { // 게임 플레이 항목 시작
            new SettingItem("난이도", "보통"), // 난이도 기본값
            new SettingItem("튜토리얼/힌트", "켜기"), // 안내 표시 기본값
            new SettingItem("힌트 빈도", "보통"), // 힌트 빈도 기본값
            new SettingItem("전투 속도", "x1.5"), // 전투 속도 기본값
            new SettingItem("애니메이션 스킵", "일부만"), // 연출 생략 기본값
            new SettingItem("다음 전투 자동 시작", "끄기"), // 전투 자동 시작 기본값
            new SettingItem("대화 자동 넘김", "끄기"), // 대화 자동 진행 기본값
            new SettingItem("전투 결과 자동 정리", "끄기"), // 전투 결과 자동 진행 기본값
            new SettingItem("데미지 숫자 표시", "켜기"), // 피해 숫자 표시 기본값
            new SettingItem("상세 툴팁 표시 방식", "항상"), // 상세 설명 기본값
            new SettingItem("전투 로그", "켜기"), // 전투 기록 기본값
            new SettingItem("이벤트 결과 요약", "켜기"), // 이벤트 요약 기본값
            new SettingItem("오토 세이브 간격", "노드마다"), // 선택 자동 저장 기본값
            new SettingItem("중요한 선택 확인창", "켜기"), // 선택 확인 기본값
            new SettingItem("런 종료/리셋 확인창", "켜기"), // 종료 확인 기본값
            new SettingItem("번쩍임 감소", "약함") // 화면 점멸 감소 기본값
        }, // 게임 플레이 항목 종료
        new SettingItem[] // 조작 설정 항목
        { // 조작 항목 시작
            new SettingItem("키 설정(리바인드)", "기본키"), // 키 설정 기본값
            new SettingItem("마우스/키보드 UI 모드", "켜기"), // 입력 안내 기본값
            new SettingItem("패드 지원", "자동"), // 패드 사용 기본값
            new SettingItem("버튼 안내 표시", "자동"), // 버튼 안내 기본값
            new SettingItem("마우스 감도/커서 속도", "6"), // 커서 속도 기본값
            new SettingItem("스크롤 속도", "보통"), // 스크롤 속도 기본값
            new SettingItem("반복 입력 방지", "켜기"), // 반복 입력 제한 기본값
            new SettingItem("기본 조작 프리셋", "기본"), // 조작 구성 기본값
            new SettingItem("진동(패드)", "보통") // 패드 진동 기본값
        }, // 조작 항목 종료
        new SettingItem[] // 언어 설정 항목
        { // 언어 항목 시작
            new SettingItem("언어 선택", "한국어"), // 게임 언어 기본값
            new SettingItem("자막 언어", "게임 언어와 동일"), // 자막 언어 기본값
            new SettingItem("자막 크기", "보통"), // 자막 크기 기본값
            new SettingItem("자막 배경", "켜기"), // 자막 배경 기본값
            new SettingItem("폰트/글꼴 설정", "기본"), // 글꼴 기본값
            new SettingItem("숫자 표기 형식", "자동(언어 기준)") // 숫자 형식 기본값
        } // 언어 항목 종료
    }; // 전체 설정 항목 종료
} // 설정 목록 종료
