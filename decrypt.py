import pyzipper
import io
import os
import argparse
import sys

def decrypt_phantom_harvest(file_path, password):
    if not os.path.exists(file_path):
        print(f"[-] 错误: 找不到文件 {file_path}")
        return

    print(f"[*] 目标文件: {file_path}")
    
    # 转换密码为 bytes
    try:
        pwd_bytes = password.encode('utf-8')
    except Exception as e:
        print(f"[-] 密码编码错误: {e}")
        return

    try:
        with pyzipper.AESZipFile(file_path, 'r') as outer_zip:
            
            # 设置密码
            outer_zip.setpassword(pwd_bytes)
            
            if 'data.bin' not in outer_zip.namelist():
                print("[-] 错误: 压缩包内未找到 'data.bin'。")
                print(f"[*] 实际包含文件: {outer_zip.namelist()}")
                return

            print("[*] 正在尝试解密并读取 data.bin ...")
            
            try:
                # 读取 data.bin
                data_bin_content = outer_zip.read('data.bin')
                print("[+] 密码正确，解密成功！")
            except RuntimeError as e:
                if 'Bad password' in str(e):
                    print("[-] 错误: 密码不正确。")
                else:
                    print(f"[-] 解密失败: {e}")
                return
            except Exception as e:
                print(f"[-] 读取错误: {e}")
                return

            # 2. 处理内层数据 (data.bin 是普通的 ZIP 流，用标准 zipfile 或 pyzipper 均可)
            print("[*] 正在解析内部数据流...")
            with pyzipper.ZipFile(io.BytesIO(data_bin_content), 'r') as inner_zip:
                # 创建输出目录
                output_dir = file_path + "_extracted"
                if not os.path.exists(output_dir):
                    os.makedirs(output_dir)
                
                # 提取所有文件
                inner_zip.extractall(output_dir)
                file_count = len(inner_zip.namelist())
                print(f"[+] 成功！已提取 {file_count} 个文件到目录:")
                print(f"    -> {os.path.abspath(output_dir)}")

    except pyzipper.BadZipFile:
        print("[-] 错误: 文件损坏或不是有效的 ZIP 格式。")
    except Exception as e:
        print(f"[-] 发生未知错误: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="PhantomHarvest 数据解密工具 (AES支持版)")
    parser.add_argument("file", help="要解密的伪装文件路径 (例如 c8f0527d252.sys)")
    parser.add_argument("password", help="生成文件时使用的密码")

    if len(sys.argv) < 3:
        parser.print_help()
        print("\n示例用法:")
        print("  python decrypt_v2.py c8f0527d252.sys mypassword123")
    else:
        args = parser.parse_args()
        decrypt_phantom_harvest(args.file, args.password)