using System.Collections.Generic; // 캐릭터 목록 사용
using UnityEngine; // 유니티 데이터 에셋 사용
[CreateAssetMenu(fileName = "CharacterRoster", menuName = "Project C/Data/Character Roster")] // 캐릭터 목록 생성 메뉴
public sealed class CharacterRosterData : ScriptableObject // 공용 캐릭터 목록 데이터
{ // 목록 클래스 시작
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>(); // 캐릭터 데이터 목록
    public IReadOnlyList<CharacterData> Characters => characters.AsReadOnly(); // 읽기 전용 캐릭터 목록 조회
} // 목록 클래스 종료
