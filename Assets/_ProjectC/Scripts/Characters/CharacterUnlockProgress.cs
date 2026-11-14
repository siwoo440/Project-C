using System; // 저장 키 예외 사용
using UnityEngine; // 영구 설정 저장 사용
public sealed class CharacterUnlockProgress // 캐릭터 해금 진행 상태
{ // 해금 상태 시작
    private const string DefaultKey = "ProjectC.Progress.FinalExplorationCleared"; // 최종 탐사 완료 저장 키
    private readonly string storageKey; // 해금 상태 저장 키
    public static CharacterUnlockProgress Shared { get; } = new CharacterUnlockProgress(DefaultKey); // 공용 해금 상태
    public CharacterUnlockProgress(string key) // 해금 상태 생성
    { // 생성 시작
        storageKey = !string.IsNullOrWhiteSpace(key) ? key : throw new ArgumentException("저장 키가 필요합니다.", nameof(key)); // 유효 저장 키 보관
    } // 생성 종료
    public bool IsUnlocked(CharacterData character) // 캐릭터 해금 여부 조회
    { // 해금 확인 시작
        return character != null && (character.CharacterId != "ARC_21" || PlayerPrefs.GetInt(storageKey, 0) == 1); // 세계 해금 조건 확인
    } // 해금 확인 종료
    public void RecordFinalClear() // 최종 탐사 성공 기록
    { // 기록 시작
        PlayerPrefs.SetInt(storageKey, 1); // 최종 탐사 완료 저장
        PlayerPrefs.Save(); // 해금 상태 즉시 기록
    } // 기록 종료
} // 해금 상태 종료
