import os
import sys
import docx
from generator_setup import create_document, create_main_section
from generator_prelim import build_cover_pages, build_preliminary_pages
from generator_intro import build_intro
from generator_ch1 import build_chapter_1
from generator_ch2 import build_chapter_2
from generator_specs import append_deep_specifications
from generator_ch3 import build_chapter_3_and_conclusion
from generator_post import build_post_content

def main():
    print("Initializing document with Phenikaa format...")
    doc = create_document()
    
    print("Building Cover Pages (Section 1)...")
    build_cover_pages(doc)
    
    print("Building Preliminary Pages (Section 2 - Roman)...")
    build_preliminary_pages(doc)
    
    print("Initializing Main Content Section (Section 3 - Arabic 1, 2, 3...)...")
    create_main_section(doc)
    
    print("Building Intro...")
    build_intro(doc)
    
    print("Building Chapter 1...")
    build_chapter_1(doc)
    
    print("Building Chapter 2...")
    build_chapter_2(doc)
    
    print("Building Deep Specifications & Database Schema...")
    append_deep_specifications(doc)
    
    print("Building Chapter 3 and Conclusion...")
    build_chapter_3_and_conclusion(doc)
    
    print("Building Post Content: References and Appendices...")
    build_post_content(doc)
    
    output_path = "báo cáo/Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx"
    doc.save(output_path)
    print("Document saved successfully to", output_path)

if __name__ == "__main__":
    main()
