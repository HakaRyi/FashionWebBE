import xlwt
from datetime import datetime

# ========== SearchService.cs Analysis ==========
# Function 1: GetTopInfluencersAsync (line 32-62) = 31 lines -> 3 test cases
# Function 2: GetSearchHistoryAsync (line 64-76) = 13 lines -> 1 test case (min 1)
# Function 3: AddSearchHistoryAsync (line 78-97) = 20 lines -> 2 test cases
# Function 4: ClearSearchHistoryAsync (line 99-102) = 4 lines -> 1 test case (min 1)
# Function 5: SearchUsersAsync (line 104-136) = 33 lines -> 3 test cases

wb = xlwt.Workbook()

# ===== Styles =====
header_font = xlwt.Font()
header_font.bold = True
header_font.name = 'Arial'
header_font.height = 220

normal_font = xlwt.Font()
normal_font.name = 'Arial'
normal_font.height = 200

border_thin = xlwt.Borders()
border_thin.left = xlwt.Borders.THIN
border_thin.right = xlwt.Borders.THIN
border_thin.top = xlwt.Borders.THIN
border_thin.bottom = xlwt.Borders.THIN

header_style = xlwt.XFStyle()
header_style.font = header_font
header_style.borders = border_thin

normal_style = xlwt.XFStyle()
normal_style.font = normal_font
normal_style.borders = border_thin

center_style = xlwt.XFStyle()
center_style.font = normal_font
center_style.borders = border_thin
center_style.alignment = xlwt.Alignment()
center_style.alignment.horz = xlwt.Alignment.HORZ_CENTER

# Light yellow for header area
pattern_yellow = xlwt.Pattern()
pattern_yellow.pattern = xlwt.Pattern.SOLID_PATTERN
pattern_yellow.pattern_fore_colour = xlwt.Style.colour_map['light_yellow']
header_yellow = xlwt.XFStyle()
header_yellow.font = header_font
header_yellow.borders = border_thin
header_yellow.pattern = pattern_yellow

# Light green for passed
pattern_green = xlwt.Pattern()
pattern_green.pattern = xlwt.Pattern.SOLID_PATTERN
pattern_green.pattern_fore_colour = xlwt.Style.colour_map['light_green']
green_style = xlwt.XFStyle()
green_style.font = normal_font
green_style.borders = border_thin
green_style.pattern = pattern_green

def create_sheet(wb, sheet_name, func_code, func_name, lines_of_code, num_tc,
                 test_requirement, preconditions, inputs, outputs, test_matrix, result_types):
    """
    Create a sheet following the template format.
    inputs: list of dict {'name': str, 'values': list of str}
    outputs: list of dict {'name': str, 'values': list of str}
    test_matrix: dict mapping (section_idx, value_idx) -> list of tc indices (0-based)
    result_types: list of 'N'/'A'/'B' per test case
    """
    ws = wb.add_sheet(sheet_name)

    max_col = max(4 + num_tc, 19)

    # Column widths
    ws.col(0).width = 3500
    ws.col(1).width = 4500
    ws.col(2).width = 3000
    ws.col(3).width = 5500
    ws.col(4).width = 1500
    for c in range(5, max_col + 1):
        ws.col(c).width = 2800

    # Row 1: Function Code / Function Name
    ws.write_merge(0, 0, 0, 1, 'Function Code', header_yellow)
    ws.write_merge(0, 0, 2, 4, func_code, normal_style)
    ws.write_merge(0, 0, 5, 10, 'Function Name', header_yellow)
    ws.write_merge(0, 0, 11, max_col, func_name, normal_style)

    # Row 2: Created By / Executed By
    ws.write_merge(1, 1, 0, 1, 'Created By', header_yellow)
    ws.write_merge(1, 1, 2, 4, 'Developer', normal_style)
    ws.write_merge(1, 1, 5, 10, 'Executed By', header_yellow)
    ws.write_merge(1, 1, 11, max_col, '', normal_style)

    # Row 3: Lines of code / Lack of test cases
    ws.write_merge(2, 2, 0, 1, 'Lines of code', header_yellow)
    ws.write_merge(2, 2, 2, 3, lines_of_code, normal_style)
    ws.write_merge(2, 2, 5, 10, 'Lack of test cases', header_yellow)
    ws.write_merge(2, 2, 11, max_col, 0, normal_style)

    # Row 4: Test requirement
    ws.write_merge(3, 3, 0, 1, 'Test requirement', header_yellow)
    ws.write_merge(3, 3, 2, max_col, test_requirement, normal_style)

    # Row 5: Passed / Failed / Untested / N/A/B / Total
    ws.write_merge(4, 4, 0, 1, 'Passed', header_yellow)
    ws.write_merge(4, 4, 2, 4, 'Failed', header_yellow)
    ws.write_merge(4, 4, 5, 10, 'Untested', header_yellow)
    ws.write_merge(4, 4, 11, 13, 'N/A/B', header_yellow)
    ws.write_merge(4, 4, 14, max_col, 'Total Test Cases', header_yellow)

    # Row 6: Counts
    ws.write_merge(5, 5, 0, 1, 0, normal_style)
    ws.write_merge(5, 5, 2, 4, 0, normal_style)
    ws.write_merge(5, 5, 5, 10, num_tc, normal_style)
    ws.write(5, 11, 0, normal_style)
    ws.write(5, 12, 0, normal_style)
    ws.write(5, 13, 0, normal_style)
    ws.write_merge(5, 5, 14, max_col, num_tc, normal_style)

    # Row 8 (index 7): UTCID headers
    ws.write(7, 0, '', normal_style)
    ws.write(7, 1, '', normal_style)
    ws.write(7, 2, '', normal_style)
    ws.write(7, 3, '', normal_style)
    ws.write(7, 4, '', normal_style)
    for i in range(num_tc):
        ws.write(7, 5 + i, f'UTCID{i+1:02d}', header_style)

    # Row 9 (index 8): Condition / Precondition
    ws.write(8, 0, 'Condition', header_yellow)
    ws.write_merge(8, 8, 1, 2, 'Precondition', header_yellow)
    ws.write(8, 3, '', normal_style)
    ws.write(8, 4, '', normal_style)
    for i in range(num_tc):
        ws.write(8, 5 + i, '', normal_style)

    # Row 10 (index 9): Precondition values
    cur_row = 9
    for pc in preconditions:
        ws.write(cur_row, 0, '', normal_style)
        ws.write(cur_row, 1, '', normal_style)
        ws.write(cur_row, 2, '', normal_style)
        ws.write_merge(cur_row, cur_row, 3, 4, pc, normal_style)
        for i in range(num_tc):
            ws.write(cur_row, 5 + i, '', normal_style)
        cur_row += 1

    # Blank row
    cur_row += 1

    # Input sections
    section_idx = 0
    for inp in inputs:
        # Input name row
        ws.write(cur_row, 0, '', normal_style)
        ws.write(cur_row, 1, inp['name'], header_style)
        ws.write(cur_row, 2, '', normal_style)
        ws.write(cur_row, 3, '', normal_style)
        ws.write(cur_row, 4, '', normal_style)
        for i in range(num_tc):
            ws.write(cur_row, 5 + i, '', normal_style)
        cur_row += 1

        for vi, val in enumerate(inp['values']):
            ws.write(cur_row, 0, '', normal_style)
            ws.write(cur_row, 1, '', normal_style)
            ws.write(cur_row, 2, '', normal_style)
            ws.write(cur_row, 3, val, normal_style)
            ws.write(cur_row, 4, '', normal_style)
            key = (section_idx, vi)
            for i in range(num_tc):
                if key in test_matrix and i in test_matrix[key]:
                    ws.write(cur_row, 5 + i, 'O', center_style)
                else:
                    ws.write(cur_row, 5 + i, '', normal_style)
            cur_row += 1
        section_idx += 1

    # Blank row
    cur_row += 1

    # Confirm / Return section
    ws.write(cur_row, 0, 'Confirm', header_yellow)
    ws.write(cur_row, 1, 'Return', header_style)
    ws.write(cur_row, 2, '', normal_style)
    ws.write(cur_row, 3, '', normal_style)
    ws.write(cur_row, 4, '', normal_style)
    for i in range(num_tc):
        ws.write(cur_row, 5 + i, '', normal_style)
    cur_row += 1

    for out in outputs:
        ws.write(cur_row, 0, '', normal_style)
        ws.write(cur_row, 1, '', normal_style)
        ws.write(cur_row, 2, '', normal_style)
        ws.write(cur_row, 3, out, normal_style)
        ws.write(cur_row, 4, '', normal_style)
        for i in range(num_tc):
            ws.write(cur_row, 5 + i, '', normal_style)
        cur_row += 1

    # Blank rows
    cur_row += 1
    # Exception
    ws.write(cur_row, 0, '', normal_style)
    ws.write(cur_row, 1, 'Exception', header_style)
    for c in range(2, 5 + num_tc):
        ws.write(cur_row, c, '', normal_style)
    cur_row += 2

    # Log message
    ws.write(cur_row, 0, '', normal_style)
    ws.write(cur_row, 1, 'Log message', header_style)
    for c in range(2, 5 + num_tc):
        ws.write(cur_row, c, '', normal_style)
    cur_row += 2

    # Result section
    # Type row
    ws.write(cur_row, 0, 'Result', header_yellow)
    ws.write_merge(cur_row, cur_row, 1, 4, 'Type(N : Normal, A : Abnormal, B : Boundary)', header_style)
    for i in range(num_tc):
        ws.write(cur_row, 5 + i, result_types[i] if i < len(result_types) else 'N', center_style)
    cur_row += 1

    # Passed/Failed
    ws.write(cur_row, 0, '', normal_style)
    ws.write_merge(cur_row, cur_row, 1, 4, 'Passed/Failed', header_style)
    for i in range(num_tc):
        ws.write(cur_row, 5 + i, '', center_style)
    cur_row += 1

    # Executed Date
    ws.write(cur_row, 0, '', normal_style)
    ws.write_merge(cur_row, cur_row, 1, 4, 'Executed Date', header_style)
    for i in range(num_tc):
        ws.write(cur_row, 5 + i, '', normal_style)
    cur_row += 1

    # Defect ID
    ws.write(cur_row, 0, '', normal_style)
    ws.write_merge(cur_row, cur_row, 1, 4, 'Defect ID', header_style)
    for i in range(num_tc):
        ws.write(cur_row, 5 + i, '', normal_style)

    return ws


# ==========================================
# Sheet 1: GetTopInfluencersAsync - 31 lines -> 3 TC
# ==========================================
create_sheet(wb,
    sheet_name='GetTopInfluencers',
    func_code='SearchService_01',
    func_name='GetTopInfluencersAsync',
    lines_of_code=31,
    num_tc=3,
    test_requirement='Lấy danh sách top 10 influencer có nhiều follower nhất, kèm trạng thái follow của user hiện tại',
    preconditions=['Kết nối database thành công', 'Có dữ liệu Account trong hệ thống'],
    inputs=[
        {
            'name': 'currentUserId',
            'values': ['"1" (valid userId)', '"" (empty string)', 'null']
        }
    ],
    outputs=[
        'List<UserSuggestionDto> có dữ liệu (count <= 10)',
        'List<UserSuggestionDto> rỗng',
        'Throw Exception (FormatException)'
    ],
    test_matrix={
        (0, 0): [0],      # valid userId -> TC1
        (0, 1): [1],      # empty string -> TC2
        (0, 2): [2],      # null -> TC3
    },
    result_types=['N', 'A', 'A']
)

# ==========================================
# Sheet 2: GetSearchHistoryAsync - 13 lines -> 1 TC (min 1)
# Actually let's give 2 TCs for meaningful testing
# 13/10 = 1.3 -> round to 1, but minimum 1
# ==========================================
create_sheet(wb,
    sheet_name='GetSearchHistory',
    func_code='SearchService_02',
    func_name='GetSearchHistoryAsync',
    lines_of_code=13,
    num_tc=2,
    test_requirement='Lấy lịch sử tìm kiếm của user hiện tại, tối đa 10 kết quả',
    preconditions=['Kết nối database thành công'],
    inputs=[
        {
            'name': 'currentUserId',
            'values': ['1 (userId hợp lệ, có lịch sử)', '999 (userId không có lịch sử)']
        }
    ],
    outputs=[
        'List<SearchHistoryDto> có dữ liệu (count <= 10)',
        'List<SearchHistoryDto> rỗng'
    ],
    test_matrix={
        (0, 0): [0],
        (0, 1): [1],
    },
    result_types=['N', 'A']
)

# ==========================================
# Sheet 3: AddSearchHistoryAsync - 20 lines -> 2 TC
# ==========================================
create_sheet(wb,
    sheet_name='AddSearchHistory',
    func_code='SearchService_03',
    func_name='AddSearchHistoryAsync',
    lines_of_code=20,
    num_tc=2,
    test_requirement='Thêm lịch sử tìm kiếm mới hoặc cập nhật thời gian nếu keyword đã tồn tại',
    preconditions=['Kết nối database thành công'],
    inputs=[
        {
            'name': 'currentUserId',
            'values': ['1 (userId hợp lệ)', '1 (userId hợp lệ)']
        },
        {
            'name': 'keyword',
            'values': ['"áo thun" (keyword mới, chưa tồn tại)', '"áo thun" (keyword đã tồn tại)']
        }
    ],
    outputs=[
        'Tạo mới SearchHistory thành công (AddAsync được gọi)',
        'Cập nhật CreatedAt của SearchHistory cũ (UpdateAsync được gọi)'
    ],
    test_matrix={
        (0, 0): [0],       # userId valid, keyword mới
        (0, 1): [1],       # userId valid, keyword cũ
        (1, 0): [0],       # keyword mới
        (1, 1): [1],       # keyword đã tồn tại
    },
    result_types=['N', 'N']
)

# ==========================================
# Sheet 4: ClearSearchHistoryAsync - 4 lines -> 1 TC (min 1)
# ==========================================
create_sheet(wb,
    sheet_name='ClearSearchHistory',
    func_code='SearchService_04',
    func_name='ClearSearchHistoryAsync',
    lines_of_code=4,
    num_tc=1,
    test_requirement='Xóa toàn bộ lịch sử tìm kiếm của user hiện tại',
    preconditions=['Kết nối database thành công'],
    inputs=[
        {
            'name': 'currentUserId',
            'values': ['1 (userId hợp lệ, có lịch sử tìm kiếm)']
        }
    ],
    outputs=[
        'Xóa thành công (DeleteAllByAccountIdAsync được gọi)'
    ],
    test_matrix={
        (0, 0): [0],
    },
    result_types=['N']
)

# ==========================================
# Sheet 5: SearchUsersAsync - 33 lines -> 3 TC
# ==========================================
create_sheet(wb,
    sheet_name='SearchUsers',
    func_code='SearchService_05',
    func_name='SearchUsersAsync',
    lines_of_code=33,
    num_tc=3,
    test_requirement='Tìm kiếm user theo keyword (username), trả về danh sách tối đa 20 kết quả kèm trạng thái follow',
    preconditions=['Kết nối database thành công', 'Có dữ liệu Account trong hệ thống'],
    inputs=[
        {
            'name': 'currentUserId',
            'values': ['1 (userId hợp lệ)', '1 (userId hợp lệ)', '1 (userId hợp lệ)']
        },
        {
            'name': 'keyword',
            'values': ['"john" (keyword khớp nhiều user)', '"xyznotexist" (keyword không khớp user nào)', '"J" (keyword viết hoa, test case-insensitive)']
        }
    ],
    outputs=[
        'List<UserSuggestionDto> có dữ liệu (count <= 20)',
        'List<UserSuggestionDto> rỗng',
        'List<UserSuggestionDto> có dữ liệu (case-insensitive match)'
    ],
    test_matrix={
        (0, 0): [0],
        (0, 1): [1],
        (0, 2): [2],
        (1, 0): [0],
        (1, 1): [1],
        (1, 2): [2],
    },
    result_types=['N', 'A', 'B']
)

# Save
output_path = r'd:\FPT_Environment\Ky_09\Do_An_Tot_Nghiep_PROJECT\Fashion_BE\Fashion_BackEnd_Solution\SearchService_UnitTest.xls'
wb.save(output_path)
print(f'Unit test file saved to: {output_path}')
print('Done!')
