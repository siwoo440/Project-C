#if UNITY_EDITOR // 실제 Unity 편집기가 컴파일하는 게임 어셈블리에만 적용
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")] // 편집기 회귀 검사에서 실제 내부 전투 경로 접근
#endif // Windows 플레이어 런타임의 어셈블리 접근 설정 제외
